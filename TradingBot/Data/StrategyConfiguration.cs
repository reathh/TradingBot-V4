using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TradingBot.Data
{
    public class StrategyConfiguration : IEntityTypeConfiguration<Strategy>
    {
        public void Configure(EntityTypeBuilder<Strategy> builder)
        {
            builder.HasMany(s => s.Trades)
                   .WithOne(t => t.Strategy)
                   .HasForeignKey(t => t.StrategyId);
        }
    }
}