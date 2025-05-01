namespace TradingBot.Data
{
    public class Trade(Order entryOrder)
    {
        public int Id { get; set; }
        public Order EntryOrder { get; set; } = entryOrder;
        public Order? ExitOrder { get; set; }
        public decimal? Profit { get; set; }
    }
}