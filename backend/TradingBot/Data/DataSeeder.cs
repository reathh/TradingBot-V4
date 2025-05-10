namespace TradingBot.Data
{
    public static class DataSeeder
    {
        public static void SeedDatabase(TradingBotDbContext dbContext)
        {
            // Only seed if no bots exist
            if (!dbContext.Bots.Any())
            {
                // Create a BTC/USDC bot
                var bot = new Bot(
                    id: 1,
                    name: "BTC/USDC Trading Bot",
                    publicKey: "sample-public-key",
                    privateKey: "sample-private-key"
                )
                {
                    Symbol = "BTCUSDC",
                    Enabled = true,
                    MaxPrice = 80000m,
                    MinPrice = 20000m,
                    EntryQuantity = 0.01m,
                    EntryStep = 200m,
                    ExitStep = 300m,
                    IsLong = true,
                    PlaceOrdersInAdvance = true,
                    EntryOrdersInAdvance = 5,
                    ExitOrdersInAdvance = 5,
                    StartingBaseAmount = 1m
                };

                dbContext.Bots.Add(bot);
                dbContext.SaveChanges();

                // Seed historical trades for the past 2 years
                var startDate = DateTime.UtcNow.AddYears(-2);
                var random = new Random(42); // Consistent seed for reproducible results

                for (int i = 0; i < 500; i++) // Create 500 completed trades
                {
                    var tradeDate = startDate.AddDays(random.Next(0, 730)); // Random date in the last 2 years

                    // Create entry order
                    var entryPrice = 20000m + (random.Next(0, 600) * 100m); // Price between 20,000 and 80,000
                    var entryOrder = new Order(
                        id: $"entry-{Guid.NewGuid()}",
                        symbol: "BTCUSDC",
                        price: entryPrice,
                        quantity: 0.01m,
                        isBuy: true,
                        createdAt: tradeDate
                    )
                    {
                        QuantityFilled = 0.01m,
                        AverageFillPrice = entryPrice,
                        Closed = true,
                        LastUpdated = tradeDate.AddMinutes(random.Next(1, 60))
                    };

                    // Create a trade with the entry order
                    var trade = new Trade(entryOrder)
                    {
                        BotId = bot.Id,
                        Bot = bot
                    };

                    // Create exit order
                    var exitDate = tradeDate.AddDays(random.Next(1, 14)); // Exit 1-14 days later
                    var priceChange = random.Next(-5, 15) / 100m; // -5% to +15% price change
                    var exitPrice = entryPrice * (1 + priceChange);

                    var exitOrder = new Order(
                        id: $"exit-{Guid.NewGuid()}",
                        symbol: "BTCUSDC",
                        price: exitPrice,
                        quantity: 0.01m,
                        isBuy: false,
                        createdAt: exitDate
                    )
                    {
                        QuantityFilled = 0.01m,
                        AverageFillPrice = exitPrice,
                        Closed = true,
                        LastUpdated = exitDate.AddMinutes(random.Next(1, 60))
                    };

                    trade.ExitOrder = exitOrder;
                    trade.CalculateProfit();

                    dbContext.Orders.Add(entryOrder);
                    dbContext.Orders.Add(exitOrder);
                    dbContext.Trades.Add(trade);
                }

                dbContext.SaveChanges();
            }
        }
    }
}