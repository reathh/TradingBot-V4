using Microsoft.EntityFrameworkCore;
using TradingBot.Data;

namespace TradingBot.Tests.Helpers;

public class TestDbContextFactory : IDbContextFactory<TradingBotDbContext>
{
    private readonly string _dbName;
    public TestDbContextFactory(string dbName) => _dbName = dbName;
    public TradingBotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        return new TradingBotDbContext(options);
    }
} 