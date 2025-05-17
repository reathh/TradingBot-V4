namespace TradingBot.Data
{
    public class Bot(int id, string name, string publicKey, string privateKey)
    {
        public HashSet<Trade> Trades { get; set; } = [];

        // Ownership: each bot belongs to a user
        public string OwnerId { get; set; } = string.Empty;

        public User Owner { get; set; } = null!;

        public int Id { get; set; } = id;

        public string Name { get; set; } = name;

        public string PublicKey { get; set; } = publicKey;

        public string PrivateKey { get; set; } = privateKey;

        public string Symbol { get; set; } = string.Empty;

        public bool Enabled { get; set; }

        public decimal? MaxPrice { get; set; }

        public decimal? MinPrice { get; set; }

        public bool PlaceOrdersInAdvance { get; set; }

        public bool IsLong { get; set; } = true;

        public int EntryOrdersInAdvance { get; set; } = 100;

        public int ExitOrdersInAdvance { get; set; } = 100;

        public decimal ExitStep { get; set; }

        public decimal EntryStep { get; set; }

        public decimal EntryQuantity { get; set; }

        public decimal StartingBaseAmount { get; set; } = 0;

        public decimal AvailableCapital { get; set; } = 0;

        public bool StopLossEnabled { get; set; } = false;

        public decimal StopLossPercent { get; set; } = 1.0m;

        public bool StartFromMaxPrice
        {
            get;
            set
            {
                if (MaxPrice is null && value)
                {
                    throw new InvalidOperationException($"{nameof(MaxPrice)} must be set if {nameof(StartFromMaxPrice)} is true.");
                }

                field = value;
            }
        }

        public TradingMode TradingMode { get; set; } = TradingMode.Spot;

        public OrderType EntryOrderType { get; set; } = OrderType.LimitMaker;

        public OrderType ExitOrderType { get; set; } = OrderType.LimitMaker;
    }

    public enum TradingMode
    {
        Spot,
        Margin
    }

    public enum OrderType
    {
        LimitMaker,
        Limit,
        Market
    }
}