namespace TradingBot.Data
{
    public class Trade
    {
        protected Trade() { }

        public Trade(Order entryOrder)
        {
            EntryOrder = entryOrder;
        }

        public int Id { get; set; }
        public Order EntryOrder { get; set; } = null!;
        public Order? ExitOrder { get; set; }
        public decimal? Profit { get; set; }
    }
}