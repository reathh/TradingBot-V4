using System.Collections.Concurrent;
using MediatR;
using TradingBot.Application.Common;

namespace TradingBot.Services;

public class BackgroundJobProcessor(IServiceProvider serviceProvider, ILogger<BackgroundJobProcessor> logger) : IHostedService, IBackgroundJobProcessor
{
    // Dictionary of type-specific job queues
    private readonly ConcurrentDictionary<Type, ConcurrentQueue<Func<Task>>> _typeSpecificQueues = new();

    // Dictionary of type-specific semaphores to ensure only one job of a type runs at a time
    private readonly ConcurrentDictionary<Type, SemaphoreSlim> _typeSpecificSemaphores = new();

    // Global semaphore to limit overall concurrency
    private readonly SemaphoreSlim _globalSemaphore = new(Environment.ProcessorCount);

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly AutoResetEvent _newJobEvent = new(false);

    public void Enqueue<TRequest>(TRequest request) where TRequest : IRequest<Result>
    {
        var requestType = typeof(TRequest);

        // Get or create the queue for this request type
        var queue = _typeSpecificQueues.GetOrAdd(requestType, _ => new ConcurrentQueue<Func<Task>>());

        // Get or create the semaphore for this request type
        _typeSpecificSemaphores.GetOrAdd(requestType, _ => new SemaphoreSlim(1, 1));

        // Enqueue the job
        queue.Enqueue(async () =>
        {
            using var scope = serviceProvider.CreateScope();
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
                logger.LogError(ex, "Error processing background job for command {RequestType}", typeof(TRequest).Name);
            }
        });

        // Signal that a new job is available
        _newJobEvent.Set();

        logger.LogDebug("Enqueued job of type {RequestType}, queue size: {QueueSize}", requestType.Name, queue.Count);
    }

    private static bool IsResultCommand<TRequest>(TRequest request)
    {
        if (request == null)
            return false;

        var requestType = request.GetType();
        var interfaces = requestType.GetInterfaces();

        return interfaces.Any(i => i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == typeof(IRequest<>) &&
                                   (i
                                        .GetGenericArguments()[0] ==
                                    typeof(Result) ||
                                    (i
                                         .GetGenericArguments()[0].IsGenericType &&
                                     i
                                         .GetGenericArguments()[0]
                                         .GetGenericTypeDefinition() ==
                                     typeof(Result<>))));
    }

    private async Task HandleResultCommand<TRequest>(IMediator mediator, TRequest request) where TRequest : IRequest<Result>
    {
        // Use reflection to safely call Send and get the result
        var requestType = request.GetType();
        var sendMethod = typeof(IMediator).GetMethod("Send", [requestType, typeof(CancellationToken)]);

        if (sendMethod != null)
        {
            if (sendMethod.Invoke(mediator, [request, CancellationToken.None]) is Task task)
            {
                await task;

                // Get the result from the Task using reflection
                var resultProperty = task
                    .GetType()
                    .GetProperty("Result");

                if (resultProperty != null)
                {
                    var result = resultProperty.GetValue(task);

                    switch (result)
                    {
                        case Result { Succeeded: false } resultObj:
                            logger.LogError("Command {RequestType} failed: {Errors}", typeof(TRequest).Name, string.Join(", ", resultObj.Errors));

                            break;
                        case Application.Common.IResult { Succeeded: false }:
                            logger.LogError("Command {RequestType} failed", typeof(TRequest).Name);

                            break;
                    }
                }
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => ProcessJobsAsync(_cancellationTokenSource.Token), cancellationToken);
        logger.LogInformation("Background job processor started");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        logger.LogInformation("Background job processor stopping");

        return Task.CompletedTask;
    }

    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            bool jobsProcessed = false;

            // Process all queue types
            foreach (var queueEntry in _typeSpecificQueues)
            {
                var requestType = queueEntry.Key;
                var queue = queueEntry.Value;

                // Skip empty queues
                if (queue.IsEmpty)
                    continue;

                jobsProcessed = true;

                // Get the type-specific semaphore
                if (_typeSpecificSemaphores.TryGetValue(requestType, out var typeSemaphore))
                {
                    // Try to acquire the type-specific semaphore without blocking
                    if (await typeSemaphore.WaitAsync(0, cancellationToken))
                    {
                        try
                        {
                            // Only proceed if we can acquire the global semaphore
                            await _globalSemaphore.WaitAsync(cancellationToken);

                            // Dequeue and process a job for this type
                            if (queue.TryDequeue(out var job))
                            {
                                logger.LogDebug("Processing job of type {RequestType}, remaining: {RemainingJobs}", requestType.Name, queue.Count);

                                _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await job();
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogError(ex, "Error in background job processing for {RequestType}", requestType.Name);
                                        }
                                        finally
                                        {
                                            _globalSemaphore.Release();
                                            typeSemaphore.Release();
                                        }
                                    },
                                    cancellationToken);
                            }
                            else
                            {
                                // If queue is suddenly empty, release both semaphores
                                _globalSemaphore.Release();
                                typeSemaphore.Release();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Release the type semaphore if we get canceled while waiting for global semaphore
                            typeSemaphore.Release();

                            throw;
                        }
                    }
                }
            }

            // If no jobs were processed, wait for signal or timeout
            if (!jobsProcessed)
            {
                // Wait for new job signal or timeout after 100ms
                await Task.Run(() => _newJobEvent.WaitOne(100), cancellationToken);
            }
        }

        // Dispose all semaphores
        _globalSemaphore.Dispose();

        foreach (var semaphore in _typeSpecificSemaphores.Values)
        {
            semaphore.Dispose();
        }

        _newJobEvent.Dispose();
    }
}