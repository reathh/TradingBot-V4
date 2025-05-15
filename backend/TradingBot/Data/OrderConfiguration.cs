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

            // Partial covering index: filled orders, range scans by CreatedAt
            builder.HasIndex(o => o.CreatedAt)
                   .HasDatabaseName("IX_Orders_Filled_CreatedAt")
                   .HasFilter("\"Status\" = 2")                 // partial index
               .IncludeProperties(o => new
               {
                   o.Price,
                   o.AverageFillPrice,
                   o.Quantity,
                   o.Fee
               });

        // (Optional) generic composite index if you read non-filled orders too
            builder.HasIndex(o => new { o.Status, o.CreatedAt })
                   .HasDatabaseName("IX_Orders_Status_CreatedAt");
        }
    }
}