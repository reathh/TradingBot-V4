namespace TradingBot.Models
{
    public record BotProfitDto
    {
        public string? BotId { get; init; }              // Optional, can be null for "all bots"
        public string TimePeriod { get; init; } = string.Empty;  // "2023-01-01" for day, "2023-W01" for week, etc.
        public decimal TotalProfit { get; init; }        // Aggregate profit for this time period
        public decimal TotalVolume { get; init; }        // Total trading volume 
        public int TradeCount { get; init; }             // Number of trades in this period
        public decimal WinRate { get; init; }            // Percentage of profitable trades
        public DateTime PeriodStart { get; init; }       // Start of the time period
        public DateTime PeriodEnd { get; init; }         // End of the time period
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