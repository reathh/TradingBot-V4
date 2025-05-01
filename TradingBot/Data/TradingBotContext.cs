using Microsoft.EntityFrameworkCore;

namespace TradingBot.Data
{
    public class TradingBotDbContext : DbContext
    {
        public TradingBotDbContext(DbContextOptions<TradingBotDbContext> options) : base(options) { }

        public DbSet<Bot> Bots { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Trade> Trades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingBotDbContext).Assembly);
    }
}
