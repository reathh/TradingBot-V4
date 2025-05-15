using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application.Commands.PlaceEntryOrders;

using Models;

// Note: File name is PlaceOpeningOrdersCommand.cs but class is PlaceEntryOrdersCommand
public class PlaceEntryOrdersCommand : IRequest<Result>
{
    public required TickerDto Ticker { get; init; }

    public class PlaceEntryOrdersCommandHandler(
        TradingBotDbContext dbContext,
        IExchangeApiRepository exchangeApiRepository,
        TradingNotificationService notificationService,
        ILogger<PlaceEntryOrdersCommandHandler> logger) : BaseCommandHandler<PlaceEntryOrdersCommand>(logger)
    {
        protected override async Task<Result> HandleCore(PlaceEntryOrdersCommand request, CancellationToken cancellationToken)
        {
            var botsWithQuantities = await dbContext
                .Bots
                .Where(b => b.Enabled)

                // Price range filters
                .Where(b => (b.MaxPrice == null || (b.IsLong && b.MaxPrice >= request.Ticker.Bid) || (!b.IsLong && b.MaxPrice >= request.Ticker.Ask)) &&
                            (b.MinPrice == null || (b.IsLong && b.MinPrice <= request.Ticker.Ask) || (!b.IsLong && b.MinPrice <= request.Ticker.Bid)))
                .Select(b => new
                {
                    Bot = b,
                    OpenTradesCount = b.Trades.Count(t => t.ExitOrder == null || t.ExitOrder.Status != OrderStatus.Filled),
                    HasOpenTrades = b.Trades.Any(t => t.ExitOrder == null || t.ExitOrder.Status != OrderStatus.Filled),
                    FirstTradePrice = b
                        .Trades
                        .Where(t => t.ExitOrder == null || t.ExitOrder.Status != OrderStatus.Filled)
                        .OrderBy(t => t.EntryOrder.CreatedAt)
                        .Select(t => t.EntryOrder.Price)
                        .FirstOrDefault(),
                    CurrentVolume = b
                        .Trades
                        .Where(t => t.ExitOrder == null || t.ExitOrder.Status != OrderStatus.Filled)
                        .Sum(t => t.EntryOrder.Quantity),
                    CurrentPrice = b.IsLong ? request.Ticker.Bid : request.Ticker.Ask
                })
                .Select(x => new
                {
                    x.Bot,
                    x.OpenTradesCount,
                    CatchUpQuantity = !x.HasOpenTrades
                        ? x.Bot.EntryQuantity
                        : x.Bot.IsLong

                            // Long positions: calculate difference when price moves down
                            ? Math.Max(0,
                                ((int)Math.Floor((x.FirstTradePrice - x.CurrentPrice > 0m ? x.FirstTradePrice - x.CurrentPrice : 0m) / x.Bot.EntryStep) + 1) *
                                x.Bot.EntryQuantity -
                                x.CurrentVolume)

                            // Short positions: calculate difference when price moves up
                            : Math.Max(0,
                                ((int)Math.Floor((x.CurrentPrice - x.FirstTradePrice > 0m ? (x.CurrentPrice - x.FirstTradePrice) : 0m) / x.Bot.EntryStep) + 1) *
                                x.Bot.EntryQuantity -
                                x.CurrentVolume)
                })
                .Where(x => x.CatchUpQuantity > 0 || (x.Bot.PlaceOrdersInAdvance && x.OpenTradesCount < x.Bot.EntryOrdersInAdvance))
                .ToListAsync(cancellationToken);

            List<string> errors = [];

            await Parallel.ForEachAsync(botsWithQuantities,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                },
                async (botWithQuantity, token) =>
                {
                    try
                    {
                        await PlaceOrders(dbContext,
                            botWithQuantity.Bot,
                            request.Ticker,
                            botWithQuantity.CatchUpQuantity,
                            botWithQuantity.OpenTradesCount,
                            token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Failed to place orders for bot {BotId} with quantity {Quantity}",
                            botWithQuantity.Bot.Id,
                            botWithQuantity.CatchUpQuantity);

                        lock (errors)
                        {
                            errors.Add($"Failed to place orders for bot {botWithQuantity.Bot.Id}: {ex.Message}");
                        }
                    }
                });

            return errors.Count > 0 ? Result.Failure(errors) : Result.Success;
        }

        private async Task PlaceOrders(
            TradingBotDbContext dbContext,
            Bot bot,
            TickerDto ticker,
            decimal quantity,
            int openTradesCount,
            CancellationToken cancellationToken)
        {
            var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);

            var currentPrice = bot.IsLong ? ticker.Bid : ticker.Ask;
            var stepDirection = bot.IsLong ? 1 : -1;

            if (bot.PlaceOrdersInAdvance)
            {
                var ordersToPlace = bot.EntryOrdersInAdvance - openTradesCount;

                if (ordersToPlace <= 0)
                {
                    return;
                }

                var orderTasks = new List<Task<Order>>();

                for (int i = 0; i < ordersToPlace; i++)
                {
                    var orderPrice = currentPrice - bot.EntryStep * stepDirection * i;
                    var orderQuantity = i == 0 ? quantity : bot.EntryQuantity;

                    orderTasks.Add(exchangeApi.PlaceOrder(bot, orderPrice, orderQuantity, bot.IsLong, bot.EntryOrderType, cancellationToken));
                }

                var orders = await Task.WhenAll(orderTasks);

                foreach (var order in orders)
                {
                    var trade = new Trade(order);
                    bot.Trades.Add(trade);

                    logger.LogInformation("Bot {BotId} placed {Side} order at {Price} for {Quantity} units ({OrderId})",
                        bot.Id,
                        order.IsBuy ? "buy" : "sell",
                        order.Price,
                        order.Quantity,
                        order.Id);

                    // Notify about the new order
                    await notificationService.NotifyOrderUpdated(order.Id);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // Standard behavior for bots without PlaceOrdersInAdvance
                var order = await exchangeApi.PlaceOrder(bot, currentPrice, quantity, bot.IsLong, bot.EntryOrderType, cancellationToken);

                var trade = new Trade(order);
                bot.Trades.Add(trade);

                logger.LogInformation("Bot {BotId} placed entry {Side} order at {Price} for {Quantity} units ({OrderId})",
                    bot.Id,
                    order.IsBuy ? "buy" : "sell",
                    order.Price,
                    order.Quantity,
                    order.Id);

                // Notify about the new order
                await notificationService.NotifyOrderUpdated(order.Id);

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}