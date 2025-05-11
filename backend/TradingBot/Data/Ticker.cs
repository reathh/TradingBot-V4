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
        public decimal OpenPrice { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal Volume { get; set; }
        public decimal QuoteVolume { get; set; }
        public decimal WeightedAveragePrice { get; set; }
        public decimal PriceChange { get; set; }
        public decimal PriceChangePercent { get; set; }
        public long TotalTrades { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }

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
            OpenPrice = tickerDto.OpenPrice;
            HighPrice = tickerDto.HighPrice;
            LowPrice = tickerDto.LowPrice;
            Volume = tickerDto.Volume;
            QuoteVolume = tickerDto.QuoteVolume;
            WeightedAveragePrice = tickerDto.WeightedAveragePrice;
            PriceChange = tickerDto.PriceChange;
            PriceChangePercent = tickerDto.PriceChangePercent;
            TotalTrades = tickerDto.TotalTrades;
            OpenTime = tickerDto.OpenTime;
            CloseTime = tickerDto.CloseTime;
        }
    }
} 