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

        // Get all bots that have orders that need updating
        var botsWithStaleOrders = await _dbContext.Bots
            .Where(b => b.Trades.Any(t => 
                (t.EntryOrder != null && !t.EntryOrder.Closed && t.EntryOrder.LastUpdated < cutoffTime) || 
                (t.ExitOrder != null && !t.ExitOrder.Closed && t.ExitOrder.LastUpdated < cutoffTime)))
            .Include(b => b.Trades)
                .ThenInclude(t => t.EntryOrder)
            .Include(b => b.Trades)
                .ThenInclude(t => t.ExitOrder)
            .ToListAsync(cancellationToken);
            
        if (botsWithStaleOrders.Count == 0)
        {
            _logger.LogInformation("No bots with stale orders found");
            return Result<int>.SuccessWith(0);
        }

        _logger.LogInformation("Found {BotCount} bots with stale orders", botsWithStaleOrders.Count);
        int updatedCount = 0;

        foreach (var bot in botsWithStaleOrders)
        {
            try
            {
                // Create exchange API once per bot to avoid multiple connections
                var exchangeApi = _exchangeApiRepository.GetExchangeApi(bot);
                
                // Get all stale orders for this bot
                var staleOrders = bot.Trades
                    .SelectMany(t => new[] { t.EntryOrder, t.ExitOrder })
                    .Where(o => o != null && !o.Closed && o.LastUpdated < cutoffTime)
                    .ToList();
                
                _logger.LogInformation("Processing {OrderCount} stale orders for bot {BotId} ({BotName})", 
                    staleOrders.Count, bot.Id, bot.Name);
                
                foreach (var order in staleOrders)
                {
                    try
                    {
                        // Fetch the current order status from the exchange
                        var updatedOrder = await exchangeApi.GetOrderStatus(order.Id, bot, cancellationToken);

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process stale orders for bot {BotId}", bot.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully updated {UpdatedCount} stale orders", updatedCount);

        return Result<int>.SuccessWith(updatedCount);
    }
}