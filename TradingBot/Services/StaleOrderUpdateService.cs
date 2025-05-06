using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TradingBot.Application.Commands.UpdateStaleOrders;
using TradingBot.Application.Common;

namespace TradingBot.Services;

/// <summary>
/// Background service that periodically updates stale orders
/// </summary>
public class StaleOrderUpdateService : ScheduledBackgroundService
{
    public StaleOrderUpdateService(
        IServiceProvider serviceProvider,
        ILogger<StaleOrderUpdateService> logger)
        : base(serviceProvider, logger, TimeSpan.FromMinutes(10), "Stale order update service")
    {
    }

    protected override async Task ExecuteScheduledWorkAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
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