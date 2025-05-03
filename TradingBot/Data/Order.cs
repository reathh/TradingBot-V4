namespace TradingBot.Data
{
    public class Order(string symbol, decimal price, decimal quantity, bool isBuy, DateTime createdAt)
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = symbol;
        public decimal Price { get; set; } = price;
        public decimal Quantity { get; set; } = quantity;
        public bool IsBuy { get; set; } = isBuy;
        public DateTime CreatedAt { get; set; } = createdAt;
        public string? ExchangeOrderId { get; set; }

        #region Navigation Properties
        public int? EntryTradeId { get; set; }
        public Trade? EntryTrade { get; set; } = null!;
        public int? ExitTradeId { get; set; }
        public Trade? ExitTrade { get; set; }
        #endregion
    }
}