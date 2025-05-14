namespace TradingBot.Models
{
    public record StatsDto
    {
        public string? BotId { get; init; }              // Optional, can be null for "all bots"
        public string TimePeriod { get; init; } = string.Empty;  // Formatted label for the period bucket

        // Aggregated monetary metrics for the bucket
        public decimal TotalProfit { get; init; }        // Aggregated realised profit (quote currency)
        public decimal QuoteVolume { get; init; }        // Aggregated quote-currency traded volume (Qty * Price)
        public decimal BaseVolume  { get; init; }        // Aggregated base-currency traded volume (Qty)

        // Trade statistics for the bucket
        public int      TradeCount   { get; init; }      // Number of trades executed in the bucket
        public decimal  WinRate      { get; init; }      // Percentage of winning trades (0-100)
        public decimal  ProfitPct    { get; init; }      // Profit expressed as percentage of quote volume (aka ROI)

        // Period boundaries
        public DateTime PeriodStart { get; init; }
        public DateTime PeriodEnd   { get; init; }
    }

    public enum TimeInterval
    {
        Minute,
        Hour,
        Day,
        Week,
        Month,
        Year
    }
} 