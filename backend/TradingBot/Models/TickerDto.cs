namespace TradingBot.Models
{
    public record TickerDto(string Symbol, DateTime Timestamp, decimal Bid, decimal Ask, decimal LastPrice);
} 