namespace TradingBot.Models
{
    public record TickerDto(
        string Symbol,
        DateTime Timestamp,
        decimal Bid,
        decimal Ask,
        decimal LastPrice,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal Volume,
        decimal QuoteVolume,
        decimal WeightedAveragePrice,
        decimal PriceChange,
        decimal PriceChangePercent,
        long TotalTrades,
        DateTime OpenTime,
        DateTime CloseTime
    );
} 