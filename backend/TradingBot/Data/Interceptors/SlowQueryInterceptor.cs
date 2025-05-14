using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TradingBot.Data.Interceptors;

public class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger, double warningThresholdSeconds = 0.2) : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, 
        CommandEventData eventData, 
        InterceptionResult<DbDataReader> result)
    {
        command.CommandTimeout = 30; // Default timeout in seconds
        return result;
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, 
        CommandEventData eventData, 
        InterceptionResult<DbDataReader> result, 
        CancellationToken cancellationToken = default)
    {
        command.CommandTimeout = 30; // Default timeout in seconds
        return await ValueTask.FromResult(result);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command, 
        CommandExecutedEventData eventData, 
        DbDataReader result)
    {
        LogIfSlow(command, eventData.Duration, "Query");
        return result;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, 
        CommandExecutedEventData eventData, 
        DbDataReader result, 
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration, "Async Query");
        return await ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(
        DbCommand command, 
        CommandExecutedEventData eventData, 
        int result)
    {
        LogIfSlow(command, eventData.Duration, "Non-Query");
        return result;
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, 
        CommandExecutedEventData eventData, 
        int result, 
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration, "Async Non-Query");
        return await ValueTask.FromResult(result);
    }

    public override object ScalarExecuted(
        DbCommand command, 
        CommandExecutedEventData eventData, 
        object result)
    {
        LogIfSlow(command, eventData.Duration, "Scalar");
        return result;
    }

    public override async ValueTask<object> ScalarExecutedAsync(
        DbCommand command, 
        CommandExecutedEventData eventData, 
        object result, 
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration, "Async Scalar");
        return await ValueTask.FromResult(result);
    }

    private void LogIfSlow(DbCommand command, TimeSpan duration, string operationType)
    {
        if (duration.TotalSeconds >= warningThresholdSeconds)
        {
            // Use both regular logger and Serilog for structured logging
            logger.LogWarning(
                "Slow {OperationType} detected ({Duration:F3}s): {CommandText}", 
                operationType,
                duration.TotalSeconds, 
                command.CommandText);
            
            // Use Serilog for structured logging with additional context
            Log.Warning(
                "Slow {OperationType} detected ({Duration:F3}s): {CommandText}", 
                operationType,
                duration.TotalSeconds, 
                command.CommandText);
            
            // For very slow queries, add parameters to the log for debugging
            if (duration.TotalSeconds >= warningThresholdSeconds * 5)
            {
                var parameters = string.Join(", ", 
                    command.Parameters.Cast<DbParameter>()
                        .Select(p => $"{p.ParameterName}={p.Value}"));
                
                Log.Warning(
                    "Very slow {OperationType} parameters: {Parameters}", 
                    operationType,
                    parameters);
            }
        }
    }
} 