using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application.Commands.PlaceExitOrders;

public class PlaceExitOrdersCommand : IRequest
{
    public required Ticker Ticker { get; set; }

    public class PlaceExitOrdersCommandHandler(TradingBotDbContext dbContext, IExchangeApi exchangeApi, ILogger<PlaceExitOrdersCommandHandler> logger) : IRequestHandler<PlaceExitOrdersCommand>
    {
        private readonly TradingBotDbContext db = dbContext;
        private readonly IExchangeApi exchangeApi = exchangeApi;
        private readonly ILogger<PlaceExitOrdersCommandHandler> logger = logger;

        public async Task Handle(PlaceExitOrdersCommand request, CancellationToken cancellationToken)
        {
            var botsWithTrades = await db.Bots
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
                        logger.LogError(ex, "Failed to place exit orders for bot {BotId}",
                            botWithTrades.Bot.Id);
                    }
                });
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

            if (bot.PlaceOrdersInAdvance)
            {
                var orderTasks = new List<Task<Order>>();

                foreach (var trade in eligibleTrades)
                {
                    // For each trade, place an exit order
                    var exitPrice = bot.IsLong
                        ? Math.Max(trade.EntryPrice + bot.ExitStep, currentPrice)
                        : Math.Min(trade.EntryPrice - bot.ExitStep, currentPrice);

                    orderTasks.Add(exchangeApi.PlaceOrder(
                        bot,
                        exitPrice,
                        trade.EntryQuantity,
                        !bot.IsLong, // Exit is opposite of entry direction
                        cancellationToken));
                }

                var orders = await Task.WhenAll(orderTasks);

                for (int i = 0; i < orders.Length; i++)
                {
                    var order = orders[i];
                    var trade = eligibleTrades[i].Trade;
                    trade.ExitOrder = order;

                    logger.LogInformation(
                        "Bot {BotId} placed exit {Side} order at {Price} for {Quantity} units ({OrderId})",
                        bot.Id,
                        order.IsBuy ? "buy" : "sell",
                        order.Price,
                        order.Quantity,
                        order.Id);
                }

                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Successfully placed {OrderCount} exit orders for bot {BotId}", orders.Length, bot.Id);
            }
            else
            {
                // Process one trade at a time, standard behavior
                foreach (var tradeInfo in eligibleTrades)
                {
                    var trade = tradeInfo.Trade;
                    var exitPrice = bot.IsLong
                        ? Math.Max(tradeInfo.EntryPrice + bot.ExitStep, currentPrice)
                        : Math.Min(tradeInfo.EntryPrice - bot.ExitStep, currentPrice);

                    var order = await exchangeApi.PlaceOrder(
                        bot,
                        exitPrice,
                        tradeInfo.EntryQuantity,
                        !bot.IsLong, // Exit is opposite of entry direction
                        cancellationToken);

                    trade.ExitOrder = order;

                    logger.LogInformation(
                        "Bot {BotId} placed exit {Side} order at {Price} for {Quantity} units ({OrderId})",
                        bot.Id,
                        order.IsBuy ? "buy" : "sell",
                        order.Price,
                        order.Quantity,
                        order.Id);
                }

                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Successfully placed {OrderCount} exit orders for bot {BotId}", eligibleTrades.Count, bot.Id);
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