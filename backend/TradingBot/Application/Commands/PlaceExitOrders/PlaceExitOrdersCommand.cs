using System.Collections.Concurrent;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application.Commands.PlaceExitOrders;

using Models;

public class PlaceExitOrdersCommand : IRequest<Result>
{
    public required TickerDto Ticker { get; set; }

    public class PlaceExitOrdersCommandHandler(
        TradingBotDbContext dbContext,
        IExchangeApiRepository exchangeApiRepository,
        ILogger<PlaceExitOrdersCommandHandler> logger) : BaseCommandHandler<PlaceExitOrdersCommand>(logger)
    {
        protected override async Task<Result> HandleCore(PlaceExitOrdersCommand request, CancellationToken cancellationToken)
        {
            var currentAsk = request.Ticker.Ask;
            var currentBid = request.Ticker.Bid;

            // Fetch only bots with suitable trades, and only the relevant trades for each bot
            var botsWithTrades = await dbContext.Bots
                .Where(bot => bot.Enabled)
                .Select(bot => new
                {
                    Bot = bot,
                    ConsolidatedTrades = bot.Trades
                        .Where(t =>
                            t.ExitOrder == null &&
                            t.EntryOrder.Status == OrderStatus.Filled &&
                            (
                                (bot.IsLong && ((t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) + bot.ExitStep <= currentAsk)) ||
                                (!bot.IsLong && ((t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) - bot.ExitStep >= currentBid))
                            )
                        )
                        .OrderByDescending(t => bot.IsLong ? (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) : -(t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price))
                        .Select(t => new
                        {
                            Trade = t,
                            t.EntryOrder,
                            t.ExitOrder
                        })
                        .ToList(),
                    AdvanceTrades = bot.Trades
                        .Where(t =>
                            t.ExitOrder == null &&
                            t.EntryOrder.Status == OrderStatus.Filled &&
                            (
                                (bot.IsLong && ((t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) + bot.ExitStep > currentAsk)) ||
                                (!bot.IsLong && ((t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) - bot.ExitStep < currentBid))
                            )
                        )
                        .OrderBy(t => bot.IsLong
                            // For long bots, prioritize lower entry prices since they'll become eligible sooner as price rises
                            ? (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)
                            // For short bots, prioritize higher entry prices since they'll become eligible sooner as price falls
                            : -(t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price))
                        .Take(bot.PlaceOrdersInAdvance && bot.ExitOrdersInAdvance > 0 ? bot.ExitOrdersInAdvance : 0)
                        .Select(t => new
                        {
                            Trade = t,
                            t.EntryOrder,
                            t.ExitOrder
                        })
                        .ToList()
                })
                .Where(x => x.ConsolidatedTrades.Any() || x.AdvanceTrades.Any())
                .ToListAsync(cancellationToken);

            // attach entry and exit orders to the trades
            foreach (var botWithTrades in botsWithTrades)
            {
                foreach (var tradeInfo in botWithTrades.ConsolidatedTrades)
                {
                    tradeInfo.Trade.EntryOrder = tradeInfo.EntryOrder;
                    tradeInfo.Trade.ExitOrder = tradeInfo.ExitOrder;
                }

                foreach (var tradeInfo in botWithTrades.AdvanceTrades)
                {
                    tradeInfo.Trade.EntryOrder = tradeInfo.EntryOrder;
                    tradeInfo.Trade.ExitOrder = tradeInfo.ExitOrder;
                }
            }

            ConcurrentBag<string> errors = [];

            await Parallel.ForEachAsync(botsWithTrades,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                async (botWithTrades, token) =>
                {
                    var bot = botWithTrades.Bot;
                    var consolidatedTrades = botWithTrades.ConsolidatedTrades.Select(t => t.Trade).ToList();
                    var advanceTrades = botWithTrades.AdvanceTrades.Select(t => t.Trade).ToList();

                    if (consolidatedTrades.Count == 0 && advanceTrades.Count == 0)
                    {
                        return;
                    }

                    try
                    {
                        await PlaceExitOrders(bot, request.Ticker, consolidatedTrades, advanceTrades, token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to place exit orders for bot {BotId}", bot.Id);
                        errors.Add($"Failed to place exit orders for bot {bot.Id}: {ex.Message}");
                    }
                });

            return errors.IsEmpty
                ? Result.Success
                : Result.Failure(errors);
        }

        private async Task PlaceExitOrders(Bot bot, TickerDto ticker, IList<Trade> consolidatedTrades, IList<Trade> advanceTrades, CancellationToken cancellationToken)
        {
            var currentPrice = bot.IsLong ? ticker.Ask : ticker.Bid;
            var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);
            var orderTasks = new List<(Task<Order> OrderTask, List<Trade> AssociatedTrades)>();

            // Prepare consolidated exit order for eligible trades
            if (consolidatedTrades.Any())
            {
                var targetExitPrice = bot.IsLong
                    ? Math.Max(consolidatedTrades.Min(t => t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) + bot.ExitStep, currentPrice)
                    : Math.Min(consolidatedTrades.Max(t => t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) - bot.ExitStep, currentPrice);

                var exitQuantity = consolidatedTrades.Sum(t => t.EntryOrder.QuantityFilled);

                var orderTask = exchangeApi.PlaceOrder(
                    bot,
                    targetExitPrice,
                    exitQuantity,
                    !bot.IsLong,
                    cancellationToken);

                orderTasks.Add((orderTask, consolidatedTrades.ToList()));
            }

            // Prepare advance exit orders
            foreach (var trade in advanceTrades)
            {
                var exitPrice = bot.IsLong
                    ? (trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price) + bot.ExitStep
                    : (trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price) - bot.ExitStep;

                var orderTask = exchangeApi.PlaceOrder(
                    bot,
                    exitPrice,
                    trade.EntryOrder.QuantityFilled,
                    !bot.IsLong,
                    cancellationToken);

                orderTasks.Add((orderTask, [trade]));
            }

            // Execute all orders in parallel
            var orderResults = await Task.WhenAll(
                orderTasks.Select(async item => {
                    try {
                        var order = await item.OrderTask;
                        return (Order: order, Trades: item.AssociatedTrades, Success: true);
                    }
                    catch (Exception ex) {
                        logger.LogError(ex, "Failed to place exit order for bot {BotId}", bot.Id);
                        return (Order: null!, Trades: item.AssociatedTrades, Success: false);
                    }
                })
            );

            // Process successful orders
            foreach (var result in orderResults.Where(r => r.Success && r.Order != null))
            {
                foreach (var trade in result.Trades)
                {
                    trade.ExitOrder = result.Order;
                    
                    logger.LogInformation(
                        "Bot {BotId} placed exit {Side} order at {Price} for trade with entry price {EntryPrice}",
                        bot.Id,
                        result.Order.IsBuy ? "buy" : "sell",
                        result.Order.Price,
                        trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var successfulOrderCount = orderResults.Count(r => r.Success && r.Order != null);
            var failedOrderCount = orderResults.Length - successfulOrderCount;

            if (successfulOrderCount > 0)
            {
                logger.LogInformation("Successfully placed {OrderCount} exit orders for bot {BotId}", 
                    successfulOrderCount, bot.Id);
            }
            
            if (failedOrderCount > 0)
            {
                logger.LogWarning("Failed to place {OrderCount} exit orders for bot {BotId}",
                    failedOrderCount, bot.Id);
            }
        }
    }
}