using System.Collections.Concurrent;
using System.Reflection;
using MediatR;
using TradingBot.Application.Common;

namespace TradingBot.Services;

public class BackgroundJobProcessor(IServiceProvider serviceProvider, ILogger<BackgroundJobProcessor> logger) : IHostedService, IBackgroundJobProcessor
{
    private readonly ConcurrentQueue<Func<Task>> _jobQueue = new();
    private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<BackgroundJobProcessor> _logger = logger;

    public void Enqueue<TRequest>(TRequest request) where TRequest : IRequest
    {
        _jobQueue.Enqueue(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                // Check if the request returns a Result or Result<T>
                if (IsResultCommand(request))
                {
                    // Handle commands that return a Result
                    await HandleResultCommand(mediator, request);
                }
                else
                {
                    // For commands returning void or other non-Result types
                    await mediator.Send(request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing background job for command {RequestType}",
                    typeof(TRequest).Name);
            }
        });
    }

    private static bool IsResultCommand<TRequest>(TRequest request)
    {
        if (request == null) return false;

        var requestType = request.GetType();
        var interfaces = requestType.GetInterfaces();

        return interfaces.Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IRequest<>) &&
            (
                i.GetGenericArguments()[0] == typeof(Result) ||
                (i.GetGenericArguments()[0].IsGenericType &&
                i.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(Result<>))
            ));
    }

    private async Task HandleResultCommand<TRequest>(IMediator mediator, TRequest request) where TRequest : IRequest
    {
        if (request == null) return;

        // Use reflection to safely call Send and get the result
        var requestType = request.GetType();
        var sendMethod = typeof(IMediator).GetMethod("Send", [requestType, typeof(CancellationToken)]);

        if (sendMethod != null)
        {
            var task = sendMethod.Invoke(mediator, [request, CancellationToken.None]) as Task;
            if (task != null)
            {
                await task;

                // Get the result from the Task using reflection
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    var result = resultProperty.GetValue(task);

                    // Check if the result is a Result type
                    if (result is Result resultObj && !resultObj.Succeeded)
                    {
                        _logger.LogError("Command {RequestType} failed: {Errors}",
                            typeof(TRequest).Name,
                            string.Join(", ", resultObj.Errors));
                    }
                    else if (result is Application.Common.IResult iresult && !iresult.Succeeded)
                    {
                        _logger.LogError("Command {RequestType} failed", typeof(TRequest).Name);
                    }
                }
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => ProcessJobsAsync(_cancellationTokenSource.Token), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_jobQueue.TryDequeue(out var job))
            {
                await _semaphore.WaitAsync(cancellationToken);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await job();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in background job processing");
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, cancellationToken);
            }
            else
            {
                await Task.Delay(100, cancellationToken); // Avoid busy waiting
            }
        }
    }
}
