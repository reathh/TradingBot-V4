using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
    TradingNotificationService notificationService,
    ILogger<UpdateStaleOrdersCommandHandler> logger) : BaseCommandHandler<UpdateStaleOrdersCommand, int>(logger)
{
    private sealed class NullTradingNotificationService() : TradingNotificationService(null!, NullLogger<TradingNotificationService>.Instance)
    {
        public new Task NotifyOrderUpdated(string orderId) => Task.CompletedTask;
    }

    protected override async Task<Result<int>> HandleCore(UpdateStaleOrdersCommand request, CancellationToken cancellationToken)
    {
        var currentTime = timeProvider.GetUtcNow().DateTime;
        var cutoffTime = currentTime - request.StaleThreshold;

        // Fetch only stale orders, and get the associated bot via EntryTrade or ExitTrades
        var staleOrders = await dbContext.Orders
            .Where(o => o.Status != OrderStatus.Filled && o.Status != OrderStatus.Canceled && o.LastUpdated < cutoffTime)
            .Where(o => o.EntryTrade != null || o.ExitTrades.Any()) // Ensure a related trade exists
            .Select(o => new
            {
                Order = o,
                Bot = o.EntryTrade != null
                    ? o.EntryTrade.Bot
                    : o.ExitTrades.Select(et => et.Bot).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        if (staleOrders.Count == 0)
        {
            logger.LogInformation("No stale orders found");
            return Result<int>.SuccessWith(0);
        }

        logger.LogInformation("Found {OrderCount} stale orders", staleOrders.Count);
        int updatedCount = 0;

        // Group by Bot for exchange API reuse
        var ordersByBot = staleOrders.GroupBy(x => x.Bot);

        var updateTasks = ordersByBot.SelectMany(group =>
        {
            var bot = group.Key;
            var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);
            return group.Select(async x =>
            {
                var order = x.Order;
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
                    
                    await notificationService.NotifyOrderUpdated(order.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to get status for order {OrderId} from the exchange", order.Id);
                    order.LastUpdated = currentTime;
                }
            });
        }).ToList();

        await Task.WhenAll(updateTasks);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Successfully updated {UpdatedCount} stale orders", updatedCount);
        
        return Result<int>.SuccessWith(updatedCount);
    }
}