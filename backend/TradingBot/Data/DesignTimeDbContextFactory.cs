using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TradingBot.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TradingBotDbContext>
{
    public TradingBotDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var builder = new DbContextOptionsBuilder<TradingBotDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("Connection string not found");

        Console.WriteLine($"Using connection string: {connectionString}");
        
        builder.UseNpgsql(connectionString);

        return new TradingBotDbContext(builder.Options);
    }
} 