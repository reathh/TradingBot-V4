namespace TradingBot.Data
{
    using Models;

    public class Ticker
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal LastPrice { get; set; }

        // Default constructor for EF Core
        public Ticker() { }

        // Create from a TickerDto model
        public Ticker(TickerDto tickerDto)
        {
            Symbol = tickerDto.Symbol;
            Timestamp = tickerDto.Timestamp;
            Bid = tickerDto.Bid;
            Ask = tickerDto.Ask;
            LastPrice = tickerDto.LastPrice;
        }
    }
} 