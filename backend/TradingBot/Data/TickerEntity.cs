using System;

namespace TradingBot.Data
{
    public class TickerEntity
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal LastPrice { get; set; }

        // Default constructor for EF Core
        public TickerEntity() { }

        // Create from a Ticker model
        public TickerEntity(Ticker ticker)
        {
            Symbol = ticker.Symbol;
            Timestamp = ticker.Timestamp;
            Bid = ticker.Bid;
            Ask = ticker.Ask;
            LastPrice = ticker.LastPrice;
        }
    }
} 