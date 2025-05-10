using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application.Commands.PlaceExitOrders;

public class PlaceExitOrdersCommand : IRequest<Result>
{
    public required Ticker Ticker { get; set; }

    public class PlaceExitOrdersCommandHandler(
        TradingBotDbContext dbContext,
        IExchangeApiRepository exchangeApiRepository,
        ILogger<PlaceExitOrdersCommandHandler> logger) : BaseCommandHandler<PlaceExitOrdersCommand>(logger)
    {
        private readonly TradingBotDbContext _db = dbContext;
        private readonly IExchangeApiRepository _exchangeApiRepository = exchangeApiRepository;
        private readonly ILogger<PlaceExitOrdersCommandHandler> _logger = logger;

        protected override async Task<Result> HandleCore(PlaceExitOrdersCommand request, CancellationToken cancellationToken)
        {
            var botsWithTrades = await _db.Bots
                .Where(b => b.Enabled)
                .Where(b => b.Trades.Any(t =>
                    t.ExitOrder == null &&
                    t.Profit == null &&
                    t.EntryOrder.Closed &&
                    t.EntryOrder.QuantityFilled > 0)) // Only consider trades without exit orders, where entry orders are closed and filled
                .Select(b => new
                {
                    Bot = b,
                    Trades = b.Trades
                        .Where(t =>
                            t.ExitOrder == null &&
                            t.Profit == null &&
                            t.EntryOrder.Closed &&
                            t.EntryOrder.QuantityFilled > 0)
                        .OrderBy(t => t.EntryOrder.CreatedAt)
                        .Select(t => new TradeInfo
                        {
                            Trade = t,
                            EntryPrice = t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price, // Use AverageFillPrice if available
                            EntryQuantity = t.EntryOrder.QuantityFilled // Use actual filled quantity
                        })
                        .ToList(),
                    // Current price to place exit orders
                    CurrentPrice = b.IsLong ? request.Ticker.Ask : request.Ticker.Bid
                })
                // Filter bots that have trades eligible for exit based on the current price
                .Where(x => x.Trades.Any(t => x.Bot.IsLong
                    ? t.EntryPrice + x.Bot.ExitStep <= x.CurrentPrice // Long: current price >= entry + exit step
                    : t.EntryPrice - x.Bot.ExitStep >= x.CurrentPrice)) // Short: current price <= entry - exit step
                .ToListAsync(cancellationToken);

            List<string> errors = new();

            await Parallel.ForEachAsync(botsWithTrades,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                async (botWithTrades, token) =>
                {
                    try
                    {
                        await PlaceExitOrders(botWithTrades.Bot, request.Ticker, botWithTrades.Trades, token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to place exit orders for bot {BotId}",
                            botWithTrades.Bot.Id);

                        lock (errors)
                        {
                            errors.Add($"Failed to place exit orders for bot {botWithTrades.Bot.Id}: {ex.Message}");
                        }
                    }
                });

            return errors.Count > 0
                ? Result.Failure(errors)
                : Result.Success;
        }

        private async Task PlaceExitOrders(Bot bot, Ticker ticker, IList<TradeInfo> trades, CancellationToken cancellationToken)
        {
            var currentPrice = bot.IsLong ? ticker.Ask : ticker.Bid;
            var eligibleTrades = trades.Where(t => bot.IsLong
                ? t.EntryPrice + bot.ExitStep <= currentPrice
                : t.EntryPrice - bot.ExitStep >= currentPrice).ToList();

            if (!eligibleTrades.Any())
            {
                return;
            }

            var exchangeApi = _exchangeApiRepository.GetExchangeApi(bot);

            // Group eligible trades with similar entry prices
            // For long positions, group trades with entry price <= current price - exit step
            // For short positions, group trades with entry price >= current price + exit step
            var targetExitPrice = bot.IsLong
                ? Math.Max(eligibleTrades.Min(t => t.EntryPrice) + bot.ExitStep, currentPrice)
                : Math.Min(eligibleTrades.Max(t => t.EntryPrice) - bot.ExitStep, currentPrice);

            // Calculate total quantity needed for exit
            var exitQuantity = eligibleTrades.Sum(t => t.EntryQuantity);

            // Place consolidated exit order for all eligible trades
            var consolidatedOrder = await exchangeApi.PlaceOrder(
                bot,
                targetExitPrice,
                exitQuantity,
                !bot.IsLong, // Exit is opposite of entry direction
                cancellationToken);

            // Assign the exit order to all eligible trades
            foreach (var tradeInfo in eligibleTrades)
            {
                tradeInfo.Trade.ExitOrder = consolidatedOrder;
                _logger.LogInformation(
                    "Bot {BotId} assigned exit {Side} order at {Price} for trade with entry price {EntryPrice}",
                    bot.Id,
                    consolidatedOrder.IsBuy ? "buy" : "sell",
                    consolidatedOrder.Price,
                    tradeInfo.EntryPrice);
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Successfully placed exit order for {TradeCount} trades for bot {BotId} at price {Price} for {Quantity} units",
                eligibleTrades.Count, bot.Id, consolidatedOrder.Price, consolidatedOrder.Quantity);

            // Place additional exit orders in advance if configured
            if (bot.PlaceOrdersInAdvance)
            {
                // Get trades that are filled but don't have exit orders yet and aren't eligible for exit
                var tradesWithoutExitOrders = trades
                    .Except(eligibleTrades)
                    .OrderBy(t => bot.IsLong ? t.EntryPrice : -t.EntryPrice) // Sort by entry price
                    .Take(bot.ExitOrdersInAdvance) // Limit to the number of orders in advance
                    .ToList();

                if (tradesWithoutExitOrders.Any())
                {
                    var orderTasks = new List<Task<Order>>();

                    foreach (var tradeInfo in tradesWithoutExitOrders)
                    {
                        var exitPrice = bot.IsLong
                            ? tradeInfo.EntryPrice + bot.ExitStep
                            : tradeInfo.EntryPrice - bot.ExitStep;

                        orderTasks.Add(exchangeApi.PlaceOrder(
                            bot,
                            exitPrice,
                            tradeInfo.EntryQuantity,
                            !bot.IsLong, // Exit is opposite of entry direction
                            cancellationToken));
                    }

                    var advanceOrders = await Task.WhenAll(orderTasks);

                    for (int i = 0; i < advanceOrders.Length; i++)
                    {
                        var order = advanceOrders[i];
                        var trade = tradesWithoutExitOrders[i].Trade;
                        trade.ExitOrder = order;

                        _logger.LogInformation(
                            "Bot {BotId} placed advance exit {Side} order at {Price} for {Quantity} units ({OrderId})",
                            bot.Id,
                            order.IsBuy ? "buy" : "sell",
                            order.Price,
                            order.Quantity,
                            order.Id);
                    }

                    await _db.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Successfully placed {OrderCount} advance exit orders for bot {BotId}", advanceOrders.Length, bot.Id);
                }
            }
        }
    }

    public class TradeInfo
    {
        public Trade Trade { get; set; } = null!;
        public decimal EntryPrice { get; set; }
        public decimal EntryQuantity { get; set; }
    }
}