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
    private readonly TradingBotDbContext _dbContext = dbContext;
    private readonly IExchangeApiRepository _exchangeApiRepository = exchangeApiRepository;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<UpdateStaleOrdersCommandHandler> _logger = logger;

    protected override async Task<Result<int>> HandleCore(UpdateStaleOrdersCommand request, CancellationToken cancellationToken)
    {
        var currentTime = _timeProvider.GetUtcNow().DateTime;
        var cutoffTime = currentTime - request.StaleThreshold;

        // Find orders that haven't been updated recently and aren't closed
        var staleOrders = await _dbContext.Orders
            .Where(o => !o.Closed)
            .Where(o => o.LastUpdated < cutoffTime)
            .Include(o => o.EntryTrade)
                .ThenInclude(t => t != null ? t.Bot : null)
            .ToListAsync(cancellationToken);

        if (staleOrders.Count == 0)
        {
            _logger.LogInformation("No stale orders found to update");
            return Result<int>.SuccessWith(0);
        }

        _logger.LogInformation("Found {OrderCount} stale orders to update", staleOrders.Count);
        int updatedCount = 0;

        foreach (var order in staleOrders)
        {
            try
            {
                // Skip orders with no associated trade or bot
                if (order.EntryTrade?.Bot == null)
                {
                    _logger.LogWarning("Skipping stale order update for {OrderId}: No associated bot found", order.Id);
                    continue;
                }

                var bot = order.EntryTrade.Bot;
                var exchangeApi = _exchangeApiRepository.GetExchangeApi(bot);

                // Check if we should use the exchange order ID or internal ID
                string orderIdToUse = order.ExchangeOrderId ?? order.Id;

                try
                {
                    // Fetch the current order status from the exchange
                    var updatedOrder = await exchangeApi.GetOrderStatus(orderIdToUse, bot, cancellationToken);

                    // Update order properties with the latest information
                    order.QuantityFilled = updatedOrder.QuantityFilled;
                    order.Closed = updatedOrder.Closed;
                    order.Canceled = updatedOrder.Canceled;
                    order.LastUpdated = currentTime;

                    if (updatedOrder.AverageFillPrice.HasValue)
                    {
                        order.AverageFillPrice = updatedOrder.AverageFillPrice;
                    }

                    _logger.LogInformation(
                        "Updated order {OrderId}: Filled {QuantityFilled}/{Quantity}, Closed: {Closed}, Canceled: {Canceled}",
                        order.Id, order.QuantityFilled, order.Quantity, order.Closed, order.Canceled);

                    updatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get status for order {OrderId} from the exchange", order.Id);

                    // Just update the timestamp so we don't continuously try to update the same failing order
                    order.LastUpdated = currentTime;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update stale order {OrderId}", order.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully updated {UpdatedCount} of {TotalCount} stale orders", updatedCount, staleOrders.Count);

        return Result<int>.SuccessWith(updatedCount);
    }
}