namespace TradingBot.Application.Common;

using MediatR;

public static class MediatorExtensions
{
    private static void HandleFailure(Result result)
    {
        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors));
        }
    }

    public static async Task<Result<TResponse>> SendAndThrowOnFailure<TResponse>(this IMediator mediator, IRequest<Result<TResponse>> request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(request, cancellationToken);

        HandleFailure(result);

        return result;
    }

    public static async Task<Result> SendAndThrowOnFailure(this IMediator mediator, IRequest<Result> request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(request, cancellationToken);

        HandleFailure(result);

        return result;
    }

    public static async Task<TResponse> SendAndHandleResult<TResponse>(this IMediator mediator, IRequest<Result<TResponse>> request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(request, cancellationToken);

        HandleFailure(result);

        return result.Data;
    }

    public static async Task SendAndHandleResult(this IMediator mediator, IRequest<Result> request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(request, cancellationToken);

        HandleFailure(result);
    }

    public static async Task<bool> SendAndLogOnFailure<TResponse>(this IMediator mediator, IRequest<Result<TResponse>> request, ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mediator.Send(request, cancellationToken);

            if (!result.Succeeded)
            {
                logger.LogError("Command {RequestType} failed: {Errors}",
                    request.GetType().Name,
                    string.Join(", ", result.Errors));
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command {RequestType}", request.GetType().Name);
            return false;
        }
    }

    public static async Task<bool> SendAndLogOnFailure(this IMediator mediator, IRequest<Result> request, ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await mediator.Send(request, cancellationToken);

            if (!result.Succeeded)
            {
                logger.LogError("Command {RequestType} failed: {Errors}",
                    request.GetType().Name,
                    string.Join(", ", result.Errors));
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command {RequestType}", request.GetType().Name);
            return false;
        }
    }
}