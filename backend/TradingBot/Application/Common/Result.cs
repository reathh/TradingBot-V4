namespace TradingBot.Application.Common;

/// <summary>
/// Standard error codes that can be used across the application
/// </summary>
public enum ErrorCode
{
    // General errors
    None = 0,
    Unknown = 1,
    ValidationError = 2,
    
    // Entity errors
    EntityNotFound = 100,
    OrderNotFound = 101,
    BotNotFound = 102,
    TradeNotFound = 103,
    
    // Exchange errors
    ExchangeError = 200,
    InsufficientFunds = 201,
    ApiKeyInvalid = 202,
    
    // Application errors
    ApplicationError = 300,
    DatabaseError = 301,
    ConcurrencyError = 302
}

public class Result : IResult
{
    private readonly List<string> _errors;
    private readonly List<ErrorCode> _errorCodes;

    // Public default constructor for DI
    public Result()
        : this(true, [], [])
    {
    }

    internal Result(bool succeeded, List<string> errors, List<ErrorCode> errorCodes)
    {
        Succeeded = succeeded;
        _errors = errors;
        _errorCodes = errorCodes;
    }

    public bool Succeeded { get; }

    public List<string> Errors => Succeeded ? [] : _errors;
    
    public List<ErrorCode> ErrorCodes => Succeeded ? [] : _errorCodes;
    
    /// <summary>
    /// Checks if the result contains a specific error code
    /// </summary>
    public bool HasErrorCode(ErrorCode code) => ErrorCodes.Contains(code);

    public static Result Success => new(true, [], []);

    public void ThrowExceptionIfNecessary()
    {
        if (Succeeded)
        {
            return;
        }

        throw new Exception(string.Join(Environment.NewLine, Errors));
    }

    public static Result Failure(IEnumerable<string> errors, IEnumerable<ErrorCode>? errorCodes = null)
        => new(false, errors.ToList(), errorCodes?.ToList() ?? [ErrorCode.Unknown]);

    public static Result Failure(string error, ErrorCode errorCode)
        => new(false, [error], [errorCode]);

    public static implicit operator Result(string error)
        => Failure(new List<string> { error }, [ErrorCode.Unknown]);

    public static implicit operator Result(List<string> errors)
        => Failure(errors.ToList(), [ErrorCode.Unknown]);

    public static implicit operator Result(bool success)
        => success ? Success : Failure(["Unsuccessful operation."], [ErrorCode.Unknown]);
        
    public static implicit operator Result((string error, ErrorCode code) errorInfo)
        => Failure(errorInfo.error, errorInfo.code);

    public static implicit operator bool(Result result)
        => result.Succeeded;
}

public class Result<TData> : Result
{
    private readonly TData _data;

    private Result(bool succeeded, TData data, List<string> errors, List<ErrorCode> errorCodes) 
        : base(succeeded, errors, errorCodes)
        => _data = data;

    public TData Data
        => Succeeded
            ? _data
            : throw new InvalidOperationException(
                $"{nameof(Data)} is not available with a failed result. Use {Errors} instead.");

    public static Result<TData> SuccessWith(TData data)
        => new(true, data, [], []);

    public new static Result<TData> Failure(IEnumerable<string> errors, IEnumerable<ErrorCode>? errorCodes = null)
        => new(false, default!, errors.ToList(), errorCodes?.ToList() ?? [ErrorCode.Unknown]);
        
    public static Result<TData> Failure(string error, ErrorCode errorCode)
        => new(false, default!, [error], [errorCode]);

    public static implicit operator Result<TData>(string error)
        => Failure(new List<string> { error }, [ErrorCode.Unknown]);

    public static implicit operator Result<TData>(List<string> errors)
        => Failure(errors, [ErrorCode.Unknown]);
        
    public static implicit operator Result<TData>((string error, ErrorCode code) errorInfo)
        => Failure(errorInfo.error, errorInfo.code);

    public static implicit operator Result<TData>(TData data)
        => SuccessWith(data);

    public static implicit operator bool(Result<TData> result)
        => result.Succeeded;
}