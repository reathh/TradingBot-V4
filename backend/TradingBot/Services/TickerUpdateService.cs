using Binance.Net.Clients;
using Binance.Net.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application;
using TradingBot.Data;

namespace TradingBot.Services;

/// <summary>
/// Background service that subscribes to Binance ticker updates and dispatches NewTickerCommand
/// </summary>
public class TickerUpdateService(
    IServiceProvider serviceProvider,
    ILogger<TickerUpdateService> logger) : ScheduledBackgroundService(serviceProvider, logger, TimeSpan.FromMinutes(5), "Ticker update service")
{
    private readonly BinanceSocketClient _socketClient = new BinanceSocketClient();
    private readonly Dictionary<string, CancellationTokenSource> _subscriptions = [];
    private readonly Lock _lock = new();

    protected override async Task OnStartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing ticker subscriptions...");
        
        // Initial subscription to active symbols when service starts
        await SubscribeToActiveSymbolsAsync(cancellationToken);
    }

    protected internal override async Task ExecuteScheduledWorkAsync(CancellationToken cancellationToken)
    {
        // Periodically check for new symbols to subscribe to
        await SubscribeToActiveSymbolsAsync(cancellationToken);
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping ticker subscriptions...");
        await UnsubscribeAllAsync();
    }

    private async Task SubscribeToActiveSymbolsAsync(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingBotDbContext>();

        // Get all unique symbols from enabled bots
        var activeSymbols = await dbContext.Bots
            .Where(b => b.Enabled)
            .Select(b => b.Symbol)
            .Distinct()
            .ToListAsync(cancellationToken);

        Logger.LogInformation("Found {SymbolCount} active symbols to monitor", activeSymbols.Count);

        foreach (var symbol in activeSymbols)
        {
            // Skip if already subscribed
            lock (_lock)
            {
                if (_subscriptions.ContainsKey(symbol)) continue;
            }

            await SubscribeToSymbolAsync(symbol, cancellationToken);
        }
    }

    private async Task SubscribeToSymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Subscribing to ticker updates for {Symbol}", symbol);

        try
        {
            // Create a token source for this subscription
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // Subscribe to ticker updates
            var subscriptionResult = await _socketClient.SpotApi.ExchangeData.SubscribeToTickerUpdatesAsync(
                symbol,
                data => HandleTickerUpdate(symbol, data.Data),
                cancellationToken);

            if (!subscriptionResult.Success)
            {
                Logger.LogError("Failed to subscribe to ticker updates for {Symbol}: {Error}", 
                    symbol, subscriptionResult.Error?.Message);
                return;
            }

            // Store the subscription details
            lock (_lock)
            {
                _subscriptions[symbol] = tokenSource;
            }

            Logger.LogInformation("Successfully subscribed to ticker updates for {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error subscribing to ticker updates for {Symbol}", symbol);
        }
    }

    private void HandleTickerUpdate(string symbol, IBinanceTick tickData)
    {
        try
        {
            // Create ticker from Binance data
            var tickerDto = new TickerDto(
                symbol: symbol,
                timestamp: DateTime.UtcNow,
                bid: tickData.BestBidPrice,
                ask: tickData.BestAskPrice,
                lastPrice: tickData.LastPrice);

            // Log ticker update
            Logger.LogDebug("Received ticker update for {Symbol}: Bid={Bid}, Ask={Ask}, Last={Last}", 
                symbol, tickerDto.Bid, tickerDto.Ask, tickerDto.LastPrice);

            // Process the ticker with a new command
            using var scope = ServiceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            // Send the command asynchronously
            _ = Task.Run(async () => 
            {
                try
                {
                    var command = new NewTickerCommand { TickerDto = tickerDto };
                    await mediator.Send(command);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error processing ticker update for {Symbol}", symbol);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling ticker update for {Symbol}", symbol);
        }
    }

    private async Task UnsubscribeAllAsync()
    {
        Logger.LogInformation("Unsubscribing from all ticker updates");

        try
        {
            List<string> symbols;

            // Create a copy of the keys to avoid modification during iteration
            lock (_lock)
            {
                symbols = _subscriptions.Keys.ToList();
            }

            foreach (var symbol in symbols)
            {
                await UnsubscribeFromSymbolAsync(symbol);
            }

            // Dispose the socket client
            await _socketClient.UnsubscribeAllAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unsubscribing from ticker updates");
        }
    }

    private Task UnsubscribeFromSymbolAsync(string symbol)
    {
        Logger.LogInformation("Unsubscribing from ticker updates for {Symbol}", symbol);

        try
        {
            CancellationTokenSource? tokenSource = null;

            // Get and remove the subscription
            lock (_lock)
            {
                if (_subscriptions.TryGetValue(symbol, out tokenSource))
                {
                    _subscriptions.Remove(symbol);
                }
            }

            // Cancel the token and dispose
            if (tokenSource != null)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }

            // Instead of calling UnsubscribeAllAsync on SpotApi.ExchangeData, use the subscription
            // we need to track which subscription ID was for this symbol and then unsubscribe
            // For now, just use the client-level unsubscribe all since we don't track subscription IDs
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unsubscribing from ticker updates for {Symbol}", symbol);
        }
        return Task.CompletedTask;
    }
} 