namespace TradingBot.Data
{
    public class Order(
        string id,
        string symbol,
        decimal price,
        decimal quantity,
        bool isBuy,
        DateTime createdAt,
        decimal quantityFilled = 0,
        decimal? averageFillPrice = null,
        decimal fee = 0,
        OrderStatus status = OrderStatus.New,
        DateTime? lastUpdated = null)
    {
        public string Id { get; set; } = id;
        public string Symbol { get; set; } = symbol;
        public decimal Price { get; set; } = price;
        public decimal Quantity { get; set; } = quantity;
        public bool IsBuy { get; set; } = isBuy;
        public DateTime CreatedAt { get; set; } = createdAt;
        public decimal QuantityFilled { get; set; } = quantityFilled;
        public decimal? AverageFillPrice { get; set; } = averageFillPrice;
        public decimal Fee { get; set; } = fee;
        public OrderStatus Status { get; set; } = status;
        public DateTime LastUpdated { get; set; } = lastUpdated ?? createdAt;

        #region Navigation Properties
        public ICollection<Trade> ExitTrades { get; set; } = [];
        public Trade? EntryTrade { get; set; }
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