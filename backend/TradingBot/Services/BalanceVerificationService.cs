using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.VerifyBotBalance;
using TradingBot.Data;

namespace TradingBot.Services;

/// <summary>
/// Background service that verifies the correct balance on exchanges for all bots
/// </summary>
public class BalanceVerificationService(
    IServiceProvider serviceProvider,
    IExchangeApiRepository exchangeApiRepository,
    ILogger<BalanceVerificationService> logger) : ScheduledBackgroundService(serviceProvider, logger, TimeSpan.FromMinutes(1), "Balance verification service")
{
    private readonly IExchangeApiRepository _exchangeApiRepository = exchangeApiRepository;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected internal override async Task ExecuteScheduledWorkAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingBotDbContext>();

        // Get all enabled bots
        var bots = await dbContext.Bots
            .Where(b => b.Enabled)
            .Include(b => b.Trades)
                .ThenInclude(t => t.EntryOrder)
            .Include(b => b.Trades)
                .ThenInclude(t => t.ExitOrder)
            .ToListAsync(cancellationToken);

        Logger.LogDebug("Verifying balances for {BotCount} enabled bots", bots.Count);

        // Process each bot in parallel
        var tasks = bots.Select(bot => VerifyBotBalanceAsync(scope, bot, cancellationToken));
        await Task.WhenAll(tasks);
    }

    internal async Task VerifyBotBalanceAsync(IServiceScope scope, Bot bot, CancellationToken cancellationToken)
    {
        // Get current price for the bot's symbol
        var ticker = await GetCurrentTickerAsync(bot, cancellationToken);
        if (ticker == null)
        {
            Logger.LogWarning("Unable to get current ticker for bot {BotId} symbol {Symbol}", bot.Id, bot.Symbol);
            return;
        }

        // Create and send the command
        var command = new VerifyBotBalanceCommand
        {
            Bot = bot,
            CurrentPrice = ticker.LastPrice
        };

        await SendCommandAndLogResult(
            scope,
            command,
            cancellationToken,
            successMessage: "", // No success message needed as command handler already logs
            failureMessage: "❌ {Errors}");
    }

    private Task<TickerDto?> GetCurrentTickerAsync(Bot bot, CancellationToken cancellationToken)
    {
        // Try to get the most recent ticker from the database
        // This assumes we have a Tickers table or other mechanism to store ticker data
        // This implementation might need to be adjusted based on how ticker data is stored

        // For now, we'll mock this by creating a ticker with the mid-price between min and max price
        decimal lastPrice;
        if (bot.MaxPrice.HasValue && bot.MinPrice.HasValue)
        {
            lastPrice = (bot.MaxPrice.Value + bot.MinPrice.Value) / 2;
        }
        else if (bot.MaxPrice.HasValue)
        {
            lastPrice = bot.MaxPrice.Value;
        }
        else if (bot.MinPrice.HasValue)
        {
            lastPrice = bot.MinPrice.Value;
        }
        else
        {
            // If no price reference, try to get from the most recent trade
            var latestTrade = bot.Trades
                .OrderByDescending(t => t.EntryOrder.CreatedAt)
                .FirstOrDefault();

            if (latestTrade?.EntryOrder != null)
            {
                lastPrice = latestTrade.EntryOrder.Price;
            }
            else
            {
                // No reference price available
                return Task.FromResult<TickerDto?>(null);
            }
        }

        // Create a ticker with the mid-price
        return Task.FromResult<TickerDto?>(new TickerDto(bot.Symbol, DateTime.UtcNow, lastPrice, lastPrice, lastPrice));
    }
}