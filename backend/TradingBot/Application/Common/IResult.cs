namespace TradingBot.Application.Common;

public interface IResult
{
    bool Succeeded { get; }
    
    List<string> Errors { get; }
    
    List<ErrorCode> ErrorCodes { get; }
    
    bool HasErrorCode(ErrorCode code);
}