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
            var ticker = request.Ticker;

            // Find all active bots for this ticker
            var activeBots = await dbContext.Bots
                .Where(b => b.Enabled && b.Symbol == ticker.Symbol)
                .Include(b => b.Trades)
                    .ThenInclude(t => t.EntryOrder)
                .ToListAsync(cancellationToken);

            if (!activeBots.Any())
            {
                return Result.Success;
            }

            var placeTasks = activeBots.Select(bot => PlaceOrdersForBot(bot, ticker, cancellationToken));
            await Task.WhenAll(placeTasks);

            return Result.Success;
        }

        private async Task PlaceOrdersForBot(Bot bot, TickerDto ticker, CancellationToken cancellationToken)
        {
            var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);

            var currentPrice = bot.IsLong ? ticker.Bid : ticker.Ask;
            var stepDirection = bot.IsLong ? 1 : -1;

            var openTradesCount = bot.Trades.Count(t => t.ExitOrder == null || t.ExitOrder.Status != OrderStatus.Filled);
            var hasOpenTrades = bot.Trades.Any(t => t.ExitOrder == null || t.ExitOrder.Status != OrderStatus.Filled);
            var firstTradePrice = bot
                .Trades
                .Where(t => t.ExitOrder == null || t.ExitOrder.Status != OrderStatus.Filled)
                .OrderBy(t => t.EntryOrder.CreatedAt)
                .Select(t => t.EntryOrder.Price)
                .FirstOrDefault();
            var currentVolume = bot
                .Trades
                .Where(t => t.ExitOrder == null || t.ExitOrder.Status != OrderStatus.Filled)
                .Sum(t => t.EntryOrder.Quantity);

            var catchUpQuantity = !hasOpenTrades
                ? bot.EntryQuantity
                : bot.IsLong

                    // Long positions: calculate difference when price moves down
                    ? Math.Max(0,
                        ((int)Math.Floor((firstTradePrice - currentPrice > 0m ? firstTradePrice - currentPrice : 0m) / bot.EntryStep) + 1) *
                        bot.EntryQuantity -
                        currentVolume)

                    // Short positions: calculate difference when price moves up
                    : Math.Max(0,
                        ((int)Math.Floor((currentPrice - firstTradePrice > 0m ? (currentPrice - firstTradePrice) : 0m) / bot.EntryStep) + 1) *
                        bot.EntryQuantity -
                        currentVolume);

            if (catchUpQuantity > 0 || (bot.PlaceOrdersInAdvance && openTradesCount < bot.EntryOrdersInAdvance))
            {
                await PlaceOrders(exchangeApi, bot, ticker, catchUpQuantity, openTradesCount, cancellationToken);
            }
        }

        private async Task PlaceOrders(
            IExchangeApi exchangeApi,
            Bot bot,
            TickerDto ticker,
            decimal quantity,
            int openTradesCount,
            CancellationToken cancellationToken)
        {
            var currentPrice = bot.IsLong ? ticker.Bid : ticker.Ask;
            var stepDirection = bot.IsLong ? 1 : -1;

            if (bot.PlaceOrdersInAdvance)
            {
                var ordersToPlace = bot.EntryOrdersInAdvance - openTradesCount;

                if (ordersToPlace <= 0)
                {
                    return;
                }

                // Create a transaction to ensure atomicity
                using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
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
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log the error and roll back the transaction
                    logger.LogError(ex, "Error placing entry orders for bot {BotId}", bot.Id);
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            else
            {
                // Standard behavior for bots without PlaceOrdersInAdvance
                // Create a transaction to ensure atomicity
                using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var order = await exchangeApi.PlaceOrder(bot, currentPrice, quantity, bot.IsLong, bot.EntryOrderType, cancellationToken);

                    var trade = new Trade(order);
                    bot.Trades.Add(trade);

                    logger.LogInformation("Bot {BotId} placed entry {Side} order at {Price} for {Quantity} units because price was {CurrentPrice} ({OrderId})",
                        bot.Id,
                        order.IsBuy ? "buy" : "sell",
                        order.AverageFillPrice > 0m ? order.AverageFillPrice : order.Price,
                        order.Quantity,
                        currentPrice,
                        order.Id);

                    // Notify about the new order
                    await notificationService.NotifyOrderUpdated(order.Id);

                    await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log the error and roll back the transaction
                    logger.LogError(ex, "Error placing entry order for bot {BotId}", bot.Id);
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }
    }
}