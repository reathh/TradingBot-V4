using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace TradingBot.Application
{
    public class BackgroundJobProcessor : IHostedService
    {
        private readonly ConcurrentQueue<Func<Task>> _jobQueue = new();
        private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount);
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public BackgroundJobProcessor()
        {
            // Initialization logic if needed
        }

        public void Enqueue<T>(T instance, Func<T, Task> job)
        {
            _jobQueue.Enqueue(() => job(instance));
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
}
