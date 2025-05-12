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
        public string? ExitOrderId { get; set; }

        // Navigation property to Bot
        public int BotId { get; set; }
        public Bot Bot { get; set; } = null!;
    }
}