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
                    .HasForeignKey<Order>(o => o.EntryTradeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ExitOrder)
                    .WithOne(o => o.ExitTrade)
                    .HasForeignKey<Order>(o => o.ExitTradeId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Bot)
                    .WithMany(b => b.Trades)
                    .HasForeignKey(t => t.BotId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
