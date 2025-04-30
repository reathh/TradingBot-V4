namespace TradingBot.Data
{
    public class Bot(int id, string name, string publicKey, string privateKey)
    {
        public int Id { get; set; } = id;
        public string Name { get; set; } = name;
        public string PublicKey { get; set; } = publicKey;
        public string PrivateKey { get; set; } = privateKey;
        public bool Enabled { get; set; }

        public HashSet<Strategy> Strategies { get; set; } = [];
    }
}