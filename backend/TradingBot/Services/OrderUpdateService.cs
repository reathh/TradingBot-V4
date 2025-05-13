using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.UpdateBotOrder;
using TradingBot.Data;

namespace TradingBot.Services;

/// <summary>
/// Background service that subscribes to order updates for all enabled bots, checking for new bots every minute
/// </summary>
public class OrderUpdateService(
    IServiceProvider serviceProvider,
    IExchangeApiRepository exchangeApiRepository,
    ILogger<OrderUpdateService> logger) : ScheduledBackgroundService(serviceProvider, logger, TimeSpan.FromMinutes(1), "Order update service")
{
    private readonly HashSet<int> _subscribedBotIds = [];
    private readonly Lock _lock = new();

    protected override Task OnStartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("OrderUpdateService started. Will check for new bots every minute.");
        return Task.CompletedTask;
    }

    protected internal override async Task ExecuteScheduledWork(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingBotDbContext>();

        // Copy the set of already subscribed bot IDs for thread safety
        HashSet<int> alreadySubscribed;
        lock (_lock)
        {
            alreadySubscribed = [.. _subscribedBotIds];
        }

        // Get only enabled bots that are not already subscribed
        var botsToSubscribe = await dbContext.Bots
            .Where(b => b.Enabled && !alreadySubscribed.Contains(b.Id))
            .ToListAsync(cancellationToken);

        foreach (var bot in botsToSubscribe)
        {
            // Subscribe to order updates for this bot
            var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);
            try
            {
                await exchangeApi.SubscribeToOrderUpdates(
                    callback: async orderUpdate => await ProcessOrderUpdate(orderUpdate, cancellationToken),
                    bot: bot,
                    cancellationToken: CancellationToken.None);

                lock (_lock)
                {
                    _subscribedBotIds.Add(bot.Id);
                }
                Logger.LogInformation("Subscribed to order updates for bot {BotId} {BotName}", bot.Id, bot.Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to subscribe to order updates for bot {BotId} {BotName}", bot.Id, bot.Name);
            }
        }
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