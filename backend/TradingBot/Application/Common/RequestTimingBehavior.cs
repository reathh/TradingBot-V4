using System.Diagnostics;
using MediatR;

namespace TradingBot.Application.Common
{
    public class RequestTimingBehavior<TRequest, TResponse>(ILogger<RequestTimingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private const double WarningThresholdSeconds = 0.2;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await next(cancellationToken);
            stopwatch.Stop();

            if (stopwatch.Elapsed.TotalSeconds > WarningThresholdSeconds)
            {
                logger.LogWarning(
                    "Request {RequestType} took {ElapsedSeconds:F3} seconds",
                    typeof(TRequest).Name,
                    stopwatch.Elapsed.TotalSeconds);
            }

            return response;
        }
    }
} 