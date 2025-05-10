using MediatR;
using TradingBot.Application.Common;

namespace TradingBot.Services;

/// <summary>
/// Base class for background services that run on a schedule
/// </summary>
public abstract class ScheduledBackgroundService(
    IServiceProvider serviceProvider,
    ILogger logger,
    TimeSpan interval,
    string serviceName) : BackgroundService
{
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly ILogger Logger = logger;
    protected readonly TimeSpan Interval = interval;
    protected readonly string ServiceName = serviceName;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("{ServiceName} starting...", ServiceName);

        try
        {
            await OnStartAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Create a scope for this execution cycle
                using var scope = ServiceProvider.CreateScope();

                try
                {
                    await ExecuteScheduledWorkAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error in {ServiceName}", ServiceName);
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown, no need to log
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in {ServiceName}", ServiceName);
        }
        finally
        {
            await OnStopAsync(stoppingToken);
            Logger.LogInformation("{ServiceName} stopped", ServiceName);
        }
    }

    /// <summary>
    /// Override to execute work at each scheduled interval
    /// </summary>
    protected internal abstract Task ExecuteScheduledWorkAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Override to perform any setup operations when the service starts
    /// </summary>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override to perform any cleanup operations when the service stops
    /// </summary>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Helper method to send a command and log the result
    /// </summary>
    protected async Task<bool> SendCommandAndLogResult<TCommand, TResult>(
        IServiceScope scope,
        TCommand command,
        CancellationToken cancellationToken,
        string successMessage = "",
        string failureMessage = "")
        where TCommand : IRequest<Result<TResult>>
    {
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(command, cancellationToken);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(successMessage))
            {
                Logger.LogInformation(successMessage, result.Data);
            }
            return true;
        }
        else
        {
            Logger.LogWarning(failureMessage ?? "Command execution failed: {Errors}",
                string.Join(", ", result.Errors));
            return false;
        }
    }

    /// <summary>
    /// Helper method to send a command with no data return and log the result
    /// </summary>
    protected async Task<bool> SendCommandAndLogResult<TCommand>(
        IServiceScope scope,
        TCommand command,
        CancellationToken cancellationToken,
        string successMessage = "",
        string failureMessage = "")
        where TCommand : IRequest<Result>
    {
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(command, cancellationToken);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(successMessage))
            {
                Logger.LogInformation(successMessage);
            }
            return true;
        }
        else
        {
            Logger.LogWarning(failureMessage ?? "Command execution failed: {Errors}",
                string.Join(", ", result.Errors));
            return false;
        }
    }
}