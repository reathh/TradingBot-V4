using Binance.Net.Clients;
using Binance.Net.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Application.Commands.SaveTicker;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Application.Commands.ExitLossTrades;

namespace TradingBot.Services;

using Models;

/// <summary>
/// Background service that subscribes to Binance ticker updates and dispatches NewTickerCommand
/// </summary>
public class TickerUpdateService(IServiceProvider serviceProvider, ILogger<TickerUpdateService> logger, IBackgroundJobProcessor backgroundJobProcessor)
    : ScheduledBackgroundService(serviceProvider, logger, TimeSpan.FromMinutes(1), "Ticker update service")
{
    private readonly BinanceSocketClient _socketClient = new();
    private readonly Dictionary<string, CancellationTokenSource> _subscriptions = [];
    private readonly Lock _lock = new();
    private readonly Dictionary<string, TickerDto> _lastTickers = [];

    protected override Task OnStartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing ticker subscriptions...");

        return Task.CompletedTask;
    }

    protected internal override async Task ExecuteScheduledWork(CancellationToken cancellationToken)
        => await SubscribeToActiveSymbols(cancellationToken);

    protected override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping ticker subscriptions...");
        await UnsubscribeAllAsync();
    }

    private async Task SubscribeToActiveSymbols(CancellationToken cancellationToken)
    {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingBotDbContext>();

        // Get the list of already subscribed symbols
        List<string> alreadySubscribed;

        lock (_lock)
        {
            alreadySubscribed = _subscriptions.Keys.ToList();
        }

        // Query only for enabled symbols that are not already subscribed
        var symbolsToSubscribe = await dbContext
            .Bots
            .Where(b => b.Enabled && !alreadySubscribed.Contains(b.Symbol))
            .Select(b => b.Symbol)
            .Distinct()
            .ToListAsync(cancellationToken);

        Logger.LogInformation("Found {SymbolCount} new symbols to subscribe to", symbolsToSubscribe.Count);

        foreach (var symbol in symbolsToSubscribe)
        {
            await SubscribeToSymbol(symbol, cancellationToken);
        }
    }

    private async Task SubscribeToSymbol(string symbol, CancellationToken cancellationToken)
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
                Logger.LogError("Failed to subscribe to ticker updates for {Symbol}: {Error}", symbol, subscriptionResult.Error?.Message);

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

    private async void HandleTickerUpdate(string symbol, IBinanceTick tickData)
    {
        try
        {
            var tickerDto = new TickerDto(
                symbol,
                DateTime.UtcNow,
                tickData.BestBidPrice,
                tickData.BestAskPrice,
                tickData.LastPrice,
                tickData.OpenPrice,
                tickData.HighPrice,
                tickData.LowPrice,
                tickData.Volume,
                tickData.QuoteVolume,
                tickData.WeightedAveragePrice,
                tickData.PriceChange,
                tickData.PriceChangePercent,
                tickData.TotalTrades,
                tickData.OpenTime,
                tickData.CloseTime
            );
            Logger.LogDebug("Received ticker update for {Symbol}: Bid={Bid}, Ask={Ask}, Last={Last}", symbol, tickerDto.Bid, tickerDto.Ask, tickerDto.LastPrice);

            bool shouldSave = false;
            lock (_lock)
            {
                if (!_lastTickers.TryGetValue(symbol, out var lastTicker) ||
                    lastTicker.Bid != tickerDto.Bid ||
                    lastTicker.Ask != tickerDto.Ask ||
                    lastTicker.LastPrice != tickerDto.LastPrice)
                {
                    shouldSave = true;
                    _lastTickers[symbol] = tickerDto;
                }
            }

            using var scope = ServiceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            if (shouldSave)
            {
                var saveCommand = new SaveTickerCommand { Ticker = tickerDto };
                await mediator.Send(saveCommand);
            }

            backgroundJobProcessor.Enqueue(new PlaceEntryOrdersCommand { Ticker = tickerDto });
            backgroundJobProcessor.Enqueue(new PlaceExitOrdersCommand { Ticker = tickerDto });
            backgroundJobProcessor.Enqueue(new ExitLossTradesCommand { Ticker = tickerDto });
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
            CancellationTokenSource? tokenSource;

            // Get and remove the subscription
            lock (_lock)
            {
                _subscriptions.Remove(symbol, out tokenSource);
            }

            // Cancel the token and dispose
            if (tokenSource != null)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unsubscribing from ticker updates for {Symbol}", symbol);
        }

        return Task.CompletedTask;
    }
}