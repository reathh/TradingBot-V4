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
        public decimal? ExitStep
        {
            get;
            set
            {
                if (field is null)
                {
                    if (ExitStepPercentage is null)
                    {
                        throw new ArgumentNullException(nameof(ExitStep), "Either ExitStep or ExitStepPercentage must be provided.");
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
                        throw new ArgumentNullException(nameof(ExitStepPercentage), "Either ExitStep or ExitStepPercentage must be provided.");
                    }
                }
                field = value;
            }
        }
    }
}