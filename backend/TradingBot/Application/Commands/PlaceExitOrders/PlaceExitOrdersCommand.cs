using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;
using System.Collections.Concurrent;

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
            var currentAsk = request.Ticker.Ask;
            var currentBid = request.Ticker.Bid;

            // Fetch only bots with suitable trades, and only the relevant trades for each bot
            var botsWithTrades = await _db.Bots
                .Where(bot => bot.Enabled)
                .Select(bot => new
                {
                    Bot = bot,
                    EligibleTrades = bot.Trades
                        .Where(t =>
                            t.ExitOrder == null &&
                            t.Profit == null &&
                            t.EntryOrder.Closed &&
                            t.EntryOrder.QuantityFilled > 0 &&
                            (
                                (bot.IsLong && ((t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) + bot.ExitStep <= currentAsk)) ||
                                (!bot.IsLong && ((t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) - bot.ExitStep >= currentBid))
                            )
                        )
                        .ToList(),
                    AdvanceCandidates = bot.PlaceOrdersInAdvance
                        ? bot.Trades
                            .Where(t =>
                                t.ExitOrder == null &&
                                t.Profit == null &&
                                t.EntryOrder.Closed &&
                                t.EntryOrder.QuantityFilled > 0 &&
                                (
                                    (bot.IsLong && ((t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) + bot.ExitStep > currentAsk)) ||
                                    (!bot.IsLong && ((t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) - bot.ExitStep < currentBid))
                                )
                            )
                            .OrderByDescending(t => bot.IsLong ? (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) : -(t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price))
                            .Take(bot.ExitOrdersInAdvance)
                            .ToList()
                        : new List<Trade>()
                })
                .Where(x => x.EligibleTrades.Any() || x.AdvanceCandidates.Any())
                .Include(x => x.Bot.Trades)
                .ThenInclude(t => t.EntryOrder)
                .ToListAsync(cancellationToken);

            ConcurrentBag<string> errors = [];

            await Parallel.ForEachAsync(botsWithTrades,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                async (botWithTrades, token) =>
                {
                    var bot = botWithTrades.Bot;
                    var eligibleTrades = botWithTrades.EligibleTrades;
                    var advanceCandidates = botWithTrades.AdvanceCandidates;

                    if (eligibleTrades.Count == 0 && advanceCandidates.Count == 0)
                    {
                        return;
                    }

                    try
                    {
                        await PlaceExitOrders(bot, request.Ticker, eligibleTrades, advanceCandidates, token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to place exit orders for bot {BotId}", bot.Id);
                        errors.Add($"Failed to place exit orders for bot {bot.Id}: {ex.Message}");
                    }
                });

            return errors.IsEmpty
                ? Result.Success
                : Result.Failure(errors);
        }

        private async Task PlaceExitOrders(Bot bot, Ticker ticker, IList<Trade> eligibleTrades, IList<Trade> advanceCandidates, CancellationToken cancellationToken)
        {
            var currentPrice = bot.IsLong ? ticker.Ask : ticker.Bid;

            // Place consolidated exit order for eligible trades
            if (eligibleTrades.Any())
            {
                var exchangeApi = _exchangeApiRepository.GetExchangeApi(bot);

                var targetExitPrice = bot.IsLong
                    ? Math.Max(eligibleTrades.Min(t => t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) + bot.ExitStep, currentPrice)
                    : Math.Min(eligibleTrades.Max(t => t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) - bot.ExitStep, currentPrice);

                var exitQuantity = eligibleTrades.Sum(t => t.EntryOrder.QuantityFilled);

                var consolidatedOrder = await exchangeApi.PlaceOrder(
                    bot,
                    targetExitPrice,
                    exitQuantity,
                    !bot.IsLong,
                    cancellationToken);

                foreach (var trade in eligibleTrades)
                {
                    trade.ExitOrder = consolidatedOrder;
                    _logger.LogInformation(
                        "Bot {BotId} assigned exit {Side} order at {Price} for trade with entry price {EntryPrice}",
                        bot.Id,
                        consolidatedOrder.IsBuy ? "buy" : "sell",
                        consolidatedOrder.Price,
                        trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price);
                }

                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Successfully placed exit order for {TradeCount} trades for bot {BotId} at price {Price} for {Quantity} units",
                    eligibleTrades.Count, bot.Id, consolidatedOrder.Price, consolidatedOrder.Quantity);
            }

            // Place advance exit orders for advance candidates
            if (advanceCandidates.Any())
            {
                var exchangeApi = _exchangeApiRepository.GetExchangeApi(bot);
                var orderTasks = new List<Task<Order>>();

                foreach (var trade in advanceCandidates)
                {
                    var exitPrice = bot.IsLong
                        ? (trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price) + bot.ExitStep
                        : (trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price) - bot.ExitStep;

                    orderTasks.Add(exchangeApi.PlaceOrder(
                        bot,
                        exitPrice,
                        trade.EntryOrder.QuantityFilled,
                        !bot.IsLong,
                        cancellationToken));
                }

                var advanceOrders = await Task.WhenAll(orderTasks);

                for (int i = 0; i < advanceOrders.Length; i++)
                {
                    var order = advanceOrders[i];
                    var trade = advanceCandidates[i];
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