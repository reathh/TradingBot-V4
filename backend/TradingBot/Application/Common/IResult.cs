namespace TradingBot.Application.Common;

public interface IResult
{
    bool Succeeded { get; }
    
    List<string> Errors { get; }
}