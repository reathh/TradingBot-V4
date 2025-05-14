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

            // Frequently filtered field
            builder.HasIndex(t => t.BotId);
        }
    }
}
