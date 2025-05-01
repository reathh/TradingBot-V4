namespace TradingBot.Data
{
    public class Bot(int id, string name, string publicKey, string privateKey)
    {
        public HashSet<Trade> Trades { get; set; } = [];

        public int Id { get; set; } = id;

        #region Parameters
        public string Name { get; set; } = name;
        public string PublicKey { get; set; } = publicKey;
        public string PrivateKey { get; set; } = privateKey;
        public bool Enabled { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal Quantity { get; set; }
        public bool PlaceOrdersInAdvance { get; set; }
        public bool IsLong { get; set; } = true;
        public int MaxOrders { get; set; } = 100;
        public decimal ExitStep { get; set; }
        public decimal EntryStep { get; set; }
        public decimal EntryQuantity { get; set; }
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
        #endregion
    }
}