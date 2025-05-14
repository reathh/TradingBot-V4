using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TradingBot.Data
{
    public class BotConfiguration : IEntityTypeConfiguration<Bot>
    {
        public void Configure(EntityTypeBuilder<Bot> builder)
        {
            builder.HasMany(b => b.Trades)
                .WithOne(t => t.Bot)
                .HasForeignKey(t => t.BotId);

            // Indexes to accelerate frequent filters used in command handlers
            // 1. Quickly retrieve active bots with a specific trading direction (long/short)
            builder.HasIndex(b => new { b.Enabled, b.IsLong });

            // 2. Support price-range predicates that involve MaxPrice/MinPrice together with IsLong
            builder.HasIndex(b => new { b.IsLong, b.MaxPrice, b.MinPrice });
        }
    }
}