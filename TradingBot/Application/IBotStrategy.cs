namespace TradingBot.Application;

public interface IBotStrategy
{
    Task OnNewTickerAsync(Ticker ticker);
}