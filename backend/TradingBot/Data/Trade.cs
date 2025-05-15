namespace TradingBot.Data
{
    public class Trade
    {
        protected Trade() { }

        public Trade(Order entryOrder)
        {
            EntryOrder = entryOrder;
            EntryOrderId = entryOrder.Id;
        }

        public int Id { get; set; }
        public string EntryOrderId { get; set; } = null!;
        public Order EntryOrder { get; set; } = null!;
        public string? ExitOrderId { get; set; }
        public Order? ExitOrder { get; set; }
        public int BotId { get; set; }
        public Bot Bot { get; set; } = null!;
        public bool StopLossExit { get; set; } = false;
        public decimal AvailableCapital { get; set; } = 0;
    }
}