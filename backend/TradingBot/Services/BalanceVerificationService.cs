using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.VerifyBotBalance;
using TradingBot.Data;
using MediatR;

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
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new VerifyBalancesCommand(), cancellationToken);
    }
}