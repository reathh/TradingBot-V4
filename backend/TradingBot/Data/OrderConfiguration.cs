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

            // Indexes to speed up frequent filtering on order creation date and status
            builder.HasIndex(o => o.CreatedAt);
            builder.HasIndex(o => new { o.CreatedAt, o.Status });

            // Additional indexes to optimise entry & exit order command queries
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => new { o.Status, o.QuantityFilled });
        }
    }
}