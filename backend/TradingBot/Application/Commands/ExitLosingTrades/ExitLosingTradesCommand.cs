namespace TradingBot.Application.Commands.ExitLosingTrades;

using System.Collections.Concurrent;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Common;
using Data;
using Models;
using Services;

/// <summary>
/// Exits trades that have moved against the entry price by more than a predefined percentage.
/// A single consolidated order is placed per-bot for all trades that should be exited on loss.
/// </summary>
public class ExitLosingTradesCommand : IRequest<Result>
{
    public required TickerDto Ticker { get; init; }

    public class ExitLossTradesCommandHandler(
        TradingBotDbContext dbContext,
        IExchangeApiRepository exchangeApiRepository,
        ISymbolInfoCache symbolInfoCache,
        TradingNotificationService notificationService,
        ILogger<ExitLossTradesCommandHandler> logger) : BaseCommandHandler<ExitLosingTradesCommand>(logger)
    {
        protected override async Task<Result> HandleCore(ExitLosingTradesCommand request, CancellationToken cancellationToken)
        {
            var currentAsk = request.Ticker.Ask;
            var currentBid = request.Ticker.Bid;

            var botsWithLossTrades = await dbContext
                .Bots
                .Where(b => b.Enabled && b.StopLossEnabled)
                .Select(bot => new
                {
                    Bot = bot,
                    LossTrades = bot
                        .Trades
                        .Where(t => t.ExitOrder == null &&
                                    t.EntryOrder.Status == OrderStatus.Filled &&
                                    (
                                        // Long: price moved down >= threshold
                                        (bot.IsLong &&
                                         currentBid <= (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) * (1 - bot.StopLossPercent / 100m)) ||

                                        // Short: price moved up >= threshold
                                        (!bot.IsLong &&
                                         currentAsk >= (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price) * (1 + bot.StopLossPercent / 100m))))
                                         .Select(t => new {
                                            Trade = t,
                                            t.EntryOrder
                                         })
                        .ToList()
                })
                .Where(x => x.LossTrades.Any())
                .ToListAsync(cancellationToken);

            if (botsWithLossTrades.Count == 0)
            {
                logger.LogTrace("No trades require loss exits at this ticker update");

                return Result.Success;
            }

            ConcurrentBag<string> errors = [];

            await Parallel.ForEachAsync(botsWithLossTrades,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                },
                async (botWithTrades, token) =>
                {
                    var bot = botWithTrades.Bot;
                    botWithTrades.LossTrades.ForEach(t => t.Trade.EntryOrder = t.EntryOrder);
                    var trades = botWithTrades.LossTrades.Select(t => t.Trade).ToList();

                    try
                    {
                        await ExitLossTrades(bot, request.Ticker, trades, token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to place loss-exit order for bot {BotId}", bot.Id);
                        errors.Add($"Failed to place loss-exit order for bot {bot.Id}: {ex.Message}");
                    }
                });

            return errors.IsEmpty ? Result.Success : Result.Failure(errors);
        }

        private async Task ExitLossTrades(Bot bot, TickerDto ticker, IList<Trade> trades, CancellationToken cancellationToken)
        {
            var symbolInfo = await symbolInfoCache.GetAsync(bot.Symbol, cancellationToken);

            // Calculate total quantity to close taking fees into account & rounding
            decimal totalQty = trades.Sum(t =>
            {
                var net = t.EntryOrder.QuantityFilled - t.EntryOrder.Fee;

                return QuantityUtils.RoundDownToStep(net, symbolInfo);
            });

            if (totalQty < symbolInfo.MinQty)
            {
                logger.LogDebug("Bot {BotId} loss-exit skipped – total quantity {Qty} below minQty {MinQty}", bot.Id, totalQty, symbolInfo.MinQty);

                return;
            }

            
            var exitPrice = bot.IsLong ? ticker.Ask : ticker.Bid;
            var isBuy = !bot.IsLong;

            var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);
            Order order;

            try
            {
                order = await exchangeApi.PlaceOrder(bot, exitPrice, totalQty, isBuy, bot.ExitOrderType, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bot {BotId}: failed to place loss-exit order", bot.Id);

                throw;
            }

            foreach (var trade in trades)
            {
                trade.ExitOrder = order;
                trade.StopLossExit = true;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Bot {BotId} placed consolidated {Side} loss-exit order {OrderId} at {Price} for {Qty} units covering {Trades} trades",
                bot.Id,
                order.IsBuy ? "buy" : "sell",
                order.Id,
                order.Price,
                order.Quantity,
                trades.Count);

            // Notify clients about the new order
            await notificationService.NotifyOrderUpdated(order.Id);
        }
    }
}