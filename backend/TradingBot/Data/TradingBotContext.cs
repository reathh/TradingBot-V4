using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;

namespace TradingBot.Data
{
    public class TradingBotDbContext : IdentityDbContext<User>
    {
        public DbSet<Bot> Bots { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Trade> Trades { get; set; } = null!;
        public DbSet<Ticker> Tickers { get; set; } = null!;

        public TradingBotDbContext(DbContextOptions<TradingBotDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure Identity tables are configured
            base.OnModelCreating(modelBuilder);
            // Apply all custom configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingBotDbContext).Assembly);

            // Ensure all DateTime properties are stored as UTC
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                        ));
                    }
                }
            }
        }
    }
}
