namespace TradingBot.Models
{
    public class DashboardSummary
    {
        public int TotalBots { get; set; }
        public int ActiveOrders { get; set; }
        public int CompletedTrades { get; set; }
        public decimal TotalProfit { get; set; }
    }
} 