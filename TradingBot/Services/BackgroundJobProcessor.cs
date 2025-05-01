using System.Collections.Concurrent;
using MediatR;

namespace TradingBot.Services;

public class BackgroundJobProcessor : IHostedService, IBackgroundJobProcessor
{
    private readonly ConcurrentQueue<Func<Task>> _jobQueue = new();
    private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IServiceProvider _serviceProvider;

    public BackgroundJobProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Enqueue<TRequest>(TRequest request) where TRequest : IRequest
    {
        _jobQueue.Enqueue(() =>
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            return mediator.Send(request);
        });
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
