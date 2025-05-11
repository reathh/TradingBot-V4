using System;

namespace TradingBot.Models
{
    public class BotProfitDto
    {
        public string? BotId { get; set; }              // Optional, can be null for "all bots"
        public string TimePeriod { get; set; } = string.Empty;  // "2023-01-01" for day, "2023-W01" for week, etc.
        public decimal TotalProfit { get; set; }        // Aggregate profit for this time period
        public decimal TotalVolume { get; set; }        // Total trading volume 
        public int TradeCount { get; set; }             // Number of trades in this period
        public decimal WinRate { get; set; }            // Percentage of profitable trades
        public DateTime PeriodStart { get; set; }       // Start of the time period
        public DateTime PeriodEnd { get; set; }         // End of the time period
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