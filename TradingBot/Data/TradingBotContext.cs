using Microsoft.EntityFrameworkCore;

namespace TradingBot.Data
{
    public class TradingBotDbContext : DbContext
    {
        public TradingBotDbContext(DbContextOptions<TradingBotDbContext> options) : base(options) { }

        public DbSet<Bot> Bots { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Trade> Trades { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingBotDbContext).Assembly);
    }
}
