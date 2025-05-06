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

        public bool IsCompleted => ExitOrder != null && ExitOrder.Closed && ExitOrder.QuantityFilled > 0;

        // Calculate profit based on filled quantities and average prices
        public void CalculateProfit()
        {
            if (EntryOrder.Closed && ExitOrder != null && ExitOrder.Closed)
            {
                var entryPrice = EntryOrder.AverageFillPrice ?? EntryOrder.Price;
                var exitPrice = ExitOrder.AverageFillPrice ?? ExitOrder.Price;
                var filledQuantity = Math.Min(EntryOrder.QuantityFilled, ExitOrder.QuantityFilled);

                if (filledQuantity <= 0)
                {
                    Profit = 0;
                    return;
                }

                // For long positions: (exitPrice - entryPrice) * quantity
                // For short positions: (entryPrice - exitPrice) * quantity
                Profit = EntryOrder.IsBuy
                    ? (exitPrice - entryPrice) * filledQuantity - EntryOrder.Fees - ExitOrder.Fees
                    : (entryPrice - exitPrice) * filledQuantity - EntryOrder.Fees - ExitOrder.Fees;
            }
        }
    }
}