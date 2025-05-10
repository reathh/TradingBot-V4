namespace TradingBot.Data
{
    public class TickerDto(string symbol, DateTime timestamp, decimal bid, decimal ask, decimal lastPrice)
    {
        public string Symbol { get; set; } = symbol;
        public DateTime Timestamp { get; set; } = timestamp;
        public decimal Bid { get; set; } = bid;
        public decimal Ask { get; set; } = ask;
        public decimal LastPrice { get; set; } = lastPrice;
    }
} 