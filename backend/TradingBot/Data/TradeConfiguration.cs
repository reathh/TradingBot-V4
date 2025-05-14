using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TradingBot.Data
{
    public class TradeConfiguration : IEntityTypeConfiguration<Trade>
    {
        public void Configure(EntityTypeBuilder<Trade> builder)
        {
            builder.HasOne(t => t.EntryOrder)
                    .WithOne(o => o.EntryTrade)
                    .HasForeignKey<Trade>(t => t.EntryOrderId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ExitOrder)
                    .WithMany(o => o.ExitTrades)
                    .HasForeignKey(t => t.ExitOrderId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Bot)
                    .WithMany(b => b.Trades)
                    .HasForeignKey(t => t.BotId)
                    .OnDelete(DeleteBehavior.Cascade);

            // Indexes to improve performance of entry/exit order queries
            // 1. Composite index to quickly find trades for a bot that still need exit orders (ExitOrderId is NULL)
            builder.HasIndex(t => new { t.BotId, t.ExitOrderId });

            // 2. Index to speed up joins from trades to their entry orders
            builder.HasIndex(t => t.EntryOrderId);

            // 3. Index to speed up joins from trades to their exit orders (and look-ups by exit order)
            builder.HasIndex(t => t.ExitOrderId);

            // 4. Covering index used by hot exit-order queries (BotId filter, ExitOrder null check, join to EntryOrder)
            builder.HasIndex(t => new { t.BotId, t.ExitOrderId, t.EntryOrderId });
        }
    }
}
