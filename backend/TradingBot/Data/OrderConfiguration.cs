using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TradingBot.Data
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.AverageFillPrice)
                .HasColumnType("numeric(18,8)");

            builder.HasIndex(o => o.CreatedAt);
            builder.HasIndex(o => new { o.Status, o.CreatedAt });
            builder.HasIndex(o => o.Status);
        }
    }
}