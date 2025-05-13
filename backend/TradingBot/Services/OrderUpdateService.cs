using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
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
    
    private readonly ConcurrentDictionary<string, OrderUpdateRetry> _pendingRetries = new();
    
    // Retry delays in milliseconds
    private static readonly int[] RetryDelays = [100, 200, 500, 1000, 2000, 5000];
    
    // Initialize ActionBlock in field initializer
    private readonly ActionBlock<OrderUpdateRetry> _retryProcessor = new(
        async retry => await retry.Service.ProcessOrderUpdate(retry.OrderUpdate, retry.CancellationToken),
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        });

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

            // Try to process the update directly
            var command = new UpdateBotOrderCommand(orderUpdate);
            var result = await mediator.Send(command, cancellationToken);

            if (result.Succeeded)
            {
                Logger.LogDebug("Order update for {OrderId} processed successfully", orderUpdate.Id);
                
                // If this update was in the retry queue, we can remove it
                _pendingRetries.TryRemove(orderUpdate.Id, out _);
            }
            else
            {
                // If it failed due to order not found, schedule a retry
                if (result.Errors.Any(e => e.Contains("not found")))
                {
                    ScheduleRetry(orderUpdate, cancellationToken);
                }
                else
                {
                    Logger.LogWarning("Failed to process order update: {Errors}",
                        string.Join(", ", result.Errors));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing order update for order {OrderId}", orderUpdate.Id);
            
            // Schedule retry in case of exception as well
            ScheduleRetry(orderUpdate, cancellationToken);
        }
    }
    
    private void ScheduleRetry(OrderUpdate orderUpdate, CancellationToken cancellationToken)
    {
        // Get existing retry or create new one
        var retry = _pendingRetries.GetOrAdd(orderUpdate.Id, 
            _ => new OrderUpdateRetry(orderUpdate, 0, cancellationToken, this));
        
        // If we've already tried all delays, give up
        if (retry.RetryCount >= RetryDelays.Length)
        {
            Logger.LogWarning("Giving up on order update for {OrderId} after {Count} retries", 
                orderUpdate.Id, retry.RetryCount);
            _pendingRetries.TryRemove(orderUpdate.Id, out _);
            return;
        }
        
        // Get the appropriate delay
        int delay = RetryDelays[retry.RetryCount];
        retry.RetryCount++;
        
        Logger.LogInformation("Scheduling retry #{Count} for order {OrderId} in {Delay}ms", 
            retry.RetryCount, orderUpdate.Id, delay);
        
        // Schedule retry after delay
        Task.Delay(delay, cancellationToken)
            .ContinueWith(_ => _retryProcessor.Post(retry), cancellationToken);
    }
    
    private class OrderUpdateRetry
    {
        public OrderUpdate OrderUpdate { get; }
        public int RetryCount { get; set; }
        public CancellationToken CancellationToken { get; }
        public OrderUpdateService Service { get; }
        
        public OrderUpdateRetry(OrderUpdate orderUpdate, int retryCount, CancellationToken cancellationToken, OrderUpdateService service)
        {
            OrderUpdate = orderUpdate;
            RetryCount = retryCount;
            CancellationToken = cancellationToken;
            Service = service;
        }
    }
}