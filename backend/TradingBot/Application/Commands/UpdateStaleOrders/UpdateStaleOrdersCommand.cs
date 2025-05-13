using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application.Commands.UpdateStaleOrders;

public record UpdateStaleOrdersCommand : IRequest<Result<int>>
{
    public TimeSpan StaleThreshold { get; init; } = TimeSpan.FromMinutes(1);
}

public class UpdateStaleOrdersCommandHandler(
    TradingBotDbContext dbContext,
    IExchangeApiRepository exchangeApiRepository,
    TimeProvider timeProvider,
    ILogger<UpdateStaleOrdersCommandHandler> logger) : BaseCommandHandler<UpdateStaleOrdersCommand, int>(logger)
{
    protected override async Task<Result<int>> HandleCore(UpdateStaleOrdersCommand request, CancellationToken cancellationToken)
    {
        var currentTime = timeProvider.GetUtcNow().DateTime;
        var cutoffTime = currentTime - request.StaleThreshold;

        // Only fetch bots and their trades with stale orders (projected)
        var botsWithStaleOrders = await dbContext.Bots
            .Where(b => b.Trades.Any(t =>
                (t.EntryOrder.Status != OrderStatus.Filled && t.EntryOrder.Status != OrderStatus.Canceled && t.EntryOrder.LastUpdated < cutoffTime) ||
                (t.ExitOrder != null && t.ExitOrder.Status != OrderStatus.Filled && t.ExitOrder.Status != OrderStatus.Canceled && t.ExitOrder.LastUpdated < cutoffTime)))
            .Select(b => new
            {
                Bot = b,
                StaleTrades = b.Trades
                    .Where(t =>
                        (t.EntryOrder.Status != OrderStatus.Filled && t.EntryOrder.Status != OrderStatus.Canceled && t.EntryOrder.LastUpdated < cutoffTime) ||
                        (t.ExitOrder != null && t.ExitOrder.Status != OrderStatus.Filled && t.ExitOrder.Status != OrderStatus.Canceled && t.ExitOrder.LastUpdated < cutoffTime))
                    .Select(t => new
                    {
                        EntryOrder = t.EntryOrder,
                        ExitOrder = t.ExitOrder
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        if (botsWithStaleOrders.Count == 0)
        {
            logger.LogInformation("No bots with stale orders found");
            return Result<int>.SuccessWith(0);
        }

        logger.LogInformation("Found {BotCount} bots with stale orders", botsWithStaleOrders.Count);
        int updatedCount = 0;

        var updateTasks = botsWithStaleOrders.Select(async botWithStale =>
        {
            var bot = botWithStale.Bot;
            var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);
            // Collect all entry and exit orders that are stale for this bot
            var staleOrders = botWithStale.StaleTrades
                .SelectMany(t => new[] { t.EntryOrder, t.ExitOrder })
                .Where(o => o != null && o.Status != OrderStatus.Filled && o.Status != OrderStatus.Canceled && o.LastUpdated < cutoffTime)
                .GroupBy(o => o.Id)
                .Select(g => g.First())
                .ToList();

            logger.LogInformation("Processing {OrderCount} stale orders for bot {BotId} ({BotName})", staleOrders.Count, bot.Id, bot.Name);

            var orderTasks = staleOrders.Select(async order =>
            {
                try
                {
                    var updatedOrder = await exchangeApi.GetOrderStatus(order.Id, bot, cancellationToken);
                    order.QuantityFilled = updatedOrder.QuantityFilled;
                    order.Status = updatedOrder.Status;
                    order.LastUpdated = currentTime;
                    if (updatedOrder.AverageFillPrice.HasValue)
                        order.AverageFillPrice = updatedOrder.AverageFillPrice;
                    if (updatedOrder.Fee.HasValue)
                        order.Fee = updatedOrder.Fee.Value;
                    logger.LogInformation("Updated order {OrderId}: Filled {QuantityFilled}/{Quantity}, Status: {Status}",
                        order.Id, order.QuantityFilled, order.Quantity, order.Status);
                    Interlocked.Increment(ref updatedCount);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to get status for order {OrderId} from the exchange", order.Id);
                    order.LastUpdated = currentTime;
                }
            });
            await Task.WhenAll(orderTasks);
        });

        await Task.WhenAll(updateTasks);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Successfully updated {UpdatedCount} stale orders", updatedCount);
        return Result<int>.SuccessWith(updatedCount);
    }
}