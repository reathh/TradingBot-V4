
using TradingBot.Data;

namespace TradingBot.Application;

public class TradingStrategy(bool isLong, decimal quantity, decimal? minPrice, decimal? maxPrice, decimal? exitStep, decimal? exitStepPercentage) : IBotStrategy
{
    public Task OnNewTickerAsync(Ticker ticker)
    {
        throw new NotImplementedException();
    }
}
