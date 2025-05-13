using TradingBot.Application.Commands.UpdateStaleOrders;

namespace TradingBot.Services;

/// <summary>
/// Background service that periodically updates stale orders
/// </summary>
public class StaleOrderUpdateScheduledService(
    IServiceProvider serviceProvider,
    ILogger<StaleOrderUpdateScheduledService> logger) : ScheduledBackgroundService(serviceProvider, logger, TimeSpan.FromMinutes(1), "Stale order update service")
{
    protected internal override async Task ExecuteScheduledWork(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        Logger.LogDebug("Running stale order update check");

        // Create and send the command using the base class helper
        var command = new UpdateStaleOrdersCommand();
        await SendCommandAndLogResult<UpdateStaleOrdersCommand, int>(
            scope,
            command,
            cancellationToken,
            "Updated {Count} stale orders",
            "Failed to update stale orders: {Errors}");
    }
}