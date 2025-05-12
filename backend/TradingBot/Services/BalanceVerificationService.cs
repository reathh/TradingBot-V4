using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.VerifyBotBalance;
using TradingBot.Data;

namespace TradingBot.Services;

using Models;

/// <summary>
/// Background service that verifies the correct balance on exchanges for all bots
/// </summary>
public class BalanceVerificationService(
    IServiceProvider serviceProvider,
    ILogger<BalanceVerificationService> logger) : ScheduledBackgroundService(serviceProvider, logger, TimeSpan.FromMinutes(1), "Balance verification service")
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected internal override async Task ExecuteScheduledWork(CancellationToken cancellationToken)
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
        // Create and send the command
        var command = new VerifyBotBalanceCommand
        {
            Bot = bot
        };

        await SendCommandAndLogResult(
            scope,
            command,
            cancellationToken,
            successMessage: "", // No success message needed as command handler already logs
            failureMessage: "❌ {Errors}");
    }
}