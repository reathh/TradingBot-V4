using Binance.Net.Clients;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Authentication;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application;
using TradingBot.Data;

namespace TradingBot.Services;

/// <summary>
/// Background service that subscribes to Binance ticker updates and dispatches NewTickerCommand
/// </summary>
public class TickerUpdateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TickerUpdateService> _logger;
    private readonly BinanceSocketClient _socketClient;
    private readonly Dictionary<string, CancellationTokenSource> _subscriptions = [];
    private readonly object _lock = new();

    public TickerUpdateService(
        IServiceProvider serviceProvider,
        ILogger<TickerUpdateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Create a socket client for subscribing to ticker updates
        _socketClient = new BinanceSocketClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ticker update service starting...");

        try
        {
            // Subscribe to ticker updates for all active symbols
            await SubscribeToActiveSymbolsAsync(stoppingToken);
            
            // Check for new symbols periodically
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                await SubscribeToActiveSymbolsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown, no need to log
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ticker update service");
        }
        finally
        {
            await UnsubscribeAllAsync();
            _logger.LogInformation("Ticker update service stopped");
        }
    }

    private async Task SubscribeToActiveSymbolsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingBotDbContext>();

        // Get all unique symbols from enabled bots
        var activeSymbols = await dbContext.Bots
            .Where(b => b.Enabled)
            .Select(b => b.Symbol)
            .Distinct()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {SymbolCount} active symbols to monitor", activeSymbols.Count);

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
        _logger.LogInformation("Subscribing to ticker updates for {Symbol}", symbol);

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
                _logger.LogError("Failed to subscribe to ticker updates for {Symbol}: {Error}", 
                    symbol, subscriptionResult.Error?.Message);
                return;
            }

            // Store the subscription details
            lock (_lock)
            {
                _subscriptions[symbol] = tokenSource;
            }

            _logger.LogInformation("Successfully subscribed to ticker updates for {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to ticker updates for {Symbol}", symbol);
        }
    }

    private void HandleTickerUpdate(string symbol, IBinanceTick tickData)
    {
        try
        {
            // Create ticker from Binance data
            var ticker = new Ticker(
                symbol: symbol,
                timestamp: DateTime.UtcNow,
                bid: tickData.BestBidPrice,
                ask: tickData.BestAskPrice,
                lastPrice: tickData.LastPrice);

            // Enqueue command to process the ticker
            _logger.LogDebug("Received ticker update for {Symbol}: Bid={Bid}, Ask={Ask}, Last={Last}", 
                symbol, ticker.Bid, ticker.Ask, ticker.LastPrice);

            // Process the ticker with a new command
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            // Send the command asynchronously
            _ = Task.Run(async () => 
            {
                try
                {
                    var command = new NewTickerCommand { Ticker = ticker };
                    await mediator.Send(command);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing ticker update for {Symbol}", symbol);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ticker update for {Symbol}", symbol);
        }
    }

    private async Task UnsubscribeAllAsync()
    {
        _logger.LogInformation("Unsubscribing from all ticker updates");

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
            _logger.LogError(ex, "Error unsubscribing from ticker updates");
        }
    }

    private async Task UnsubscribeFromSymbolAsync(string symbol)
    {
        _logger.LogInformation("Unsubscribing from ticker updates for {Symbol}", symbol);

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

            // Cancel the token and unsubscribe
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
            _logger.LogError(ex, "Error unsubscribing from ticker updates for {Symbol}", symbol);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping ticker update service");
        await UnsubscribeAllAsync();
        await base.StopAsync(cancellationToken);
    }
} 