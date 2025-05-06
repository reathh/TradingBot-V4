using MediatR;
using TradingBot.Application.Commands.UpdateStaleOrders;
using TradingBot.Application.Common;

namespace TradingBot.Services;

/// <summary>
/// Background service that periodically updates stale orders
/// </summary>
public class StaleOrderUpdateService(
    IServiceProvider serviceProvider,
    ILogger<StaleOrderUpdateService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<StaleOrderUpdateService> _logger = logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stale order update service starting...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateStaleOrdersAsync(stoppingToken);

                // Wait for the next interval
                await Task.Delay(_interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown, no need to log
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in stale order update service");
        }

        _logger.LogInformation("Stale order update service stopped");
    }

    private async Task UpdateStaleOrdersAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Running stale order update check");

            // Create a new scope for the mediator
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Send the command to update stale orders
            var command = new UpdateStaleOrdersCommand();
            var result = await mediator.Send(command, cancellationToken);

            if (result.Succeeded)
            {
                _logger.LogInformation("Updated {Count} stale orders", result.Data);
            }
            else
            {
                _logger.LogWarning("Failed to update stale orders: {Errors}",
                    string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stale orders");
        }
    }
}