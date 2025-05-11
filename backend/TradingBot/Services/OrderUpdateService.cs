using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.UpdateBotOrder;
using TradingBot.Data;

namespace TradingBot.Services;

/// <summary>
/// Background service that subscribes to order updates for all enabled bots
/// </summary>
public class OrderUpdateService(
    IServiceProvider serviceProvider,
    IExchangeApiRepository exchangeApiRepository,
    ILogger<OrderUpdateService> logger) : ScheduledBackgroundService(serviceProvider, logger, TimeSpan.FromMinutes(5), "Order update service")
{
    protected override async Task OnStartAsync(CancellationToken cancellationToken)
    {
        // Create scope specifically for startup
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingBotDbContext>();

        // Use non-cancelable token for startup subscription to ensure test stability
        var enabledBots = await dbContext.Bots
            .Where(b => b.Enabled)
            .ToListAsync(CancellationToken.None);

        Logger.LogInformation("Found {BotCount} enabled bots for order update subscriptions", enabledBots.Count);

        // Set up subscriptions for each bot
        foreach (var bot in enabledBots)
        {
            // Get the appropriate exchange API for this bot
            var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);

            await exchangeApi.SubscribeToOrderUpdates(
                callback: async orderUpdate => await ProcessOrderUpdate(orderUpdate, cancellationToken),
                bot: bot,
                cancellationToken: CancellationToken.None);

            Logger.LogInformation("Subscribed to order updates for bot {BotId} {BotName}", bot.Id, bot.Name);
        }
    }

    // This service just needs to stay alive after subscribing to order updates
    // The actual work happens in the callbacks
    protected internal override Task ExecuteScheduledWork(CancellationToken cancellationToken)
    {
        // No periodic work needed, just keep service alive
        return Task.CompletedTask;
    }

    private async Task ProcessOrderUpdate(OrderUpdate orderUpdate, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Processing order update for order {OrderId}", orderUpdate.Id);

            // Create a new scope for the mediator
            using var scope = ServiceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Send the order update to the command handler
            var command = new UpdateBotOrderCommand(orderUpdate);
            var result = await mediator.Send(command, cancellationToken);

            if (result.Succeeded)
            {
                Logger.LogDebug("Order update for {OrderId} processed successfully", orderUpdate.Id);
            }
            else
            {
                Logger.LogWarning("Failed to process order update: {Errors}",
                    string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing order update for order {OrderId}", orderUpdate.Id);
        }
    }
}