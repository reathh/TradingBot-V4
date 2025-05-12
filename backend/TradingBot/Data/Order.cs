namespace TradingBot.Data
{
    public class Order(string id, string symbol, decimal price, decimal quantity, bool isBuy, DateTime createdAt)
    {
        public string Id { get; set; } = id;
        public string Symbol { get; set; } = symbol;
        public decimal Price { get; set; } = price;
        public decimal Quantity { get; set; } = quantity;
        public bool IsBuy { get; set; } = isBuy;
        public DateTime CreatedAt { get; set; } = createdAt;
        public decimal QuantityFilled { get; set; }
        public decimal? AverageFillPrice { get; set; }
        public decimal Fees { get; set; }
        public OrderStatus Status { get; set; }
        public bool Closed { get; set; }
        public DateTime LastUpdated { get; set; } = createdAt;

        #region Navigation Properties
        public int? EntryTradeId { get; set; }
        public Trade? EntryTrade { get; set; } = null!;
        public ICollection<Trade> ExitTrades { get; set; } = [];
        #endregion
    }

    public enum OrderStatus
    {
        New,
        PartiallyFilled,
        Filled,
        Canceled,
        Rejected,
        Expired,
        PendingCancel
    }
}