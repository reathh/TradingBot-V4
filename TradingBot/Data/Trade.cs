namespace TradingBot.Data
{
    public class Trade(int id, Order entryOrder, decimal maxPrice, decimal minPrice)
    {
        public int Id { get; set; } = id;
        public Order EntryOrder { get; set; } = entryOrder;
        public Order? ExitOrder { get; set; }
        public decimal? Profit { get; set; }
        public decimal MaxPrice { get; set; } = maxPrice;
        public decimal MinPrice { get; set; } = minPrice;

        #region Navigation Properties
        public Strategy Strategy { get; set; } = null!;
        public int StrategyId { get; set; }
        #endregion
    }
}