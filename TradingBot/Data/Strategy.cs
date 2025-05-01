namespace TradingBot.Data
{
    public class Strategy
    {
        public int Id { get; set; }

        public HashSet<Trade> Trades { get; set; } = [];

        public decimal? MaxPrice { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal Quantity { get; set; }
        public bool Enabled { get; set; }
        public bool PlaceOrdersInAdvance { get; set; }
        public bool StartFromMaxPrice
        {
            get; set
            {
                if (MaxPrice is null && value)
                {
                    throw new InvalidOperationException($"{nameof(MaxPrice)} must be set if {nameof(StartFromMaxPrice)} is true.");
                }
                field = value;
            }
        }
        public decimal? ExitStep
        {
            get;
            set
            {
                if (field is null)
                {
                    if (ExitStepPercentage is null)
                    {
                        throw new InvalidOperationException($"Either {nameof(ExitStep)} or {nameof(ExitStepPercentage)} must be provided.");
                    }
                }

                field = value;
            }
        }
        public decimal? ExitStepPercentage
        {
            get;
            set
            {
                if (field is null)
                {
                    if (ExitStep is null)
                    {
                        throw new InvalidOperationException($"Either {nameof(ExitStep)} or {nameof(ExitStepPercentage)} must be provided.");
                    }
                }
                field = value;
            }
        }
    }
}