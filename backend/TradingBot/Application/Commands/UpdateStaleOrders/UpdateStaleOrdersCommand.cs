using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application.Commands.UpdateStaleOrders;

public record UpdateStaleOrdersCommand : IRequest<Result<int>>
{
    public TimeSpan StaleThreshold { get; init; } = TimeSpan.FromMinutes(10);
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

        // Get all bots that have orders that need updating
        var botsWithStaleOrders = await dbContext.Bots
            .Where(b => b.Trades.Any(t => 
                (t.EntryOrder != null && t.EntryOrder.Status != OrderStatus.Filled && t.EntryOrder.Status != OrderStatus.Canceled && t.EntryOrder.LastUpdated < cutoffTime) || 
                (t.ExitOrder != null && t.ExitOrder.Status != OrderStatus.Filled && t.ExitOrder.Status != OrderStatus.Canceled && t.ExitOrder.LastUpdated < cutoffTime)))
            .Include(b => b.Trades)
                .ThenInclude(t => t.EntryOrder)
            .Include(b => b.Trades)
                .ThenInclude(t => t.ExitOrder)
            .ToListAsync(cancellationToken);
            
        if (botsWithStaleOrders.Count == 0)
        {
            logger.LogInformation("No bots with stale orders found");
            return Result<int>.SuccessWith(0);
        }

        logger.LogInformation("Found {BotCount} bots with stale orders", botsWithStaleOrders.Count);
        int updatedCount = 0;

        foreach (var bot in botsWithStaleOrders)
        {
            try
            {
                // Create exchange API once per bot to avoid multiple connections
                var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);
                
                // Get all stale orders for this bot
                var staleOrders = bot.Trades
                    .SelectMany(t => new[] { t.EntryOrder, t.ExitOrder })
                    .Where(o => o != null && o.Status != OrderStatus.Filled && o.Status != OrderStatus.Canceled && o.LastUpdated < cutoffTime)
                    .ToList();
                
                logger.LogInformation("Processing {OrderCount} stale orders for bot {BotId} ({BotName})", 
                    staleOrders.Count, bot.Id, bot.Name);
                
                foreach (var order in staleOrders)
                {
                    try
                    {
                        // Fetch the current order status from the exchange
                        var updatedOrder = await exchangeApi.GetOrderStatus(order!.Id, bot, cancellationToken);

                        // Update order properties with the latest information
                        order.QuantityFilled = updatedOrder.QuantityFilled;
                        order.Status = updatedOrder.Status;
                        order.LastUpdated = currentTime;

                        if (updatedOrder.AverageFillPrice.HasValue)
                        {
                            order.AverageFillPrice = updatedOrder.AverageFillPrice;
                        }

                        logger.LogInformation(
                            "Updated order {OrderId}: Filled {QuantityFilled}/{Quantity}, Status: {Status}",
                            order.Id, order.QuantityFilled, order.Quantity, order.Status);

                        updatedCount++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to get status for order {OrderId} from the exchange", order!.Id);

                        // Just update the timestamp so we don't continuously try to update the same failing order
                        order.LastUpdated = currentTime;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process stale orders for bot {BotId}", bot.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Successfully updated {UpdatedCount} stale orders", updatedCount);

        return Result<int>.SuccessWith(updatedCount);
    }
}