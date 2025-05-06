using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.UpdateBotOrder;
using TradingBot.Application.Common;
using TradingBot.Data;

namespace TradingBot.Services;

/// <summary>
/// Background service that subscribes to order updates for all enabled bots
/// </summary>
public class OrderUpdateService(
    IServiceProvider serviceProvider,
    IExchangeApiRepository exchangeApiRepository,
    ILogger<OrderUpdateService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IExchangeApiRepository _exchangeApiRepository = exchangeApiRepository;
    private readonly ILogger<OrderUpdateService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order update service starting...");

        try
        {
            // Get all enabled bots and subscribe to their order updates
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TradingBotDbContext>();

            var enabledBots = await dbContext.Bots
                .Where(b => b.Enabled)
                .ToListAsync(stoppingToken);

            _logger.LogInformation("Found {BotCount} enabled bots for order update subscriptions", enabledBots.Count);

            // Set up subscriptions for each bot
            foreach (var bot in enabledBots)
            {
                // Get the appropriate exchange API for this bot
                var exchangeApi = _exchangeApiRepository.GetExchangeApi(bot);

                await exchangeApi.SubscribeToOrderUpdates(
                    callback: async orderUpdate => await ProcessOrderUpdate(orderUpdate, stoppingToken),
                    bot: bot,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("Subscribed to order updates for bot {BotId} {BotName}", bot.Id, bot.Name);
            }

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown, no need to log
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in order update service");
        }

        _logger.LogInformation("Order update service stopped");
    }

    private async Task ProcessOrderUpdate(OrderUpdate orderUpdate, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing order update for order {OrderId}", orderUpdate.Id);

            // Create a new scope for the mediator
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Send the order update to the command handler and log on failure
            var command = new UpdateBotOrderCommand(orderUpdate);
            var succeeded = await mediator.SendAndLogOnFailure(command, _logger, cancellationToken);

            if (succeeded)
            {
                _logger.LogDebug("Order update for {OrderId} processed successfully", orderUpdate.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order update for order {OrderId}", orderUpdate.Id);
        }
    }
}