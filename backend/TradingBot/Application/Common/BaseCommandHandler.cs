namespace TradingBot.Application.Common;

using MediatR;

/// <summary>
/// Base handler for commands that return Result
/// </summary>
/// <typeparam name="TRequest">Command type</typeparam>
public abstract class BaseCommandHandler<TRequest>(ILogger logger) : IRequestHandler<TRequest, Result>
    where TRequest : IRequest<Result>
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Main handle method that provides exception handling
    /// </summary>
    public async Task<Result> Handle(TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await HandleCore(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling command {CommandType}: {Message}",
                typeof(TRequest).Name, ex.Message);
            return $"Error handling {typeof(TRequest).Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// Core implementation to be provided by derived classes
    /// </summary>
    protected abstract Task<Result> HandleCore(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Base handler for commands that return Result with data
/// </summary>
/// <typeparam name="TRequest">Command type</typeparam>
/// <typeparam name="TResponse">Response data type</typeparam>
public abstract class BaseCommandHandler<TRequest, TResponse>(ILogger logger) : IRequestHandler<TRequest, Result<TResponse>>
    where TRequest : IRequest<Result<TResponse>>
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Main handle method that provides exception handling
    /// </summary>
    public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await HandleCore(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling command {CommandType}: {Message}",
                typeof(TRequest).Name, ex.Message);
            return $"Error handling {typeof(TRequest).Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// Core implementation to be provided by derived classes
    /// </summary>
    protected abstract Task<Result<TResponse>> HandleCore(TRequest request, CancellationToken cancellationToken);
}