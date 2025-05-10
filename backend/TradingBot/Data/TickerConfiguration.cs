using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TradingBot.Data
{
    public class TickerConfiguration : IEntityTypeConfiguration<Ticker>
    {
        public void Configure(EntityTypeBuilder<Ticker> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Symbol).IsRequired();
            builder.Property(t => t.Timestamp).IsRequired();
            builder.Property(t => t.Bid).HasPrecision(18, 8);
            builder.Property(t => t.Ask).HasPrecision(18, 8);
            builder.Property(t => t.LastPrice).HasPrecision(18, 8);
            
            // Create an index on Symbol and Timestamp for faster queries
            builder.HasIndex(t => new { t.Symbol, t.Timestamp });
        }
    }
} 