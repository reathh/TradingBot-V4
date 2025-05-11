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
            builder.Property(t => t.OpenPrice).HasPrecision(18, 8);
            builder.Property(t => t.HighPrice).HasPrecision(18, 8);
            builder.Property(t => t.LowPrice).HasPrecision(18, 8);
            builder.Property(t => t.Volume).HasPrecision(18, 8);
            builder.Property(t => t.QuoteVolume).HasPrecision(18, 8);
            builder.Property(t => t.WeightedAveragePrice).HasPrecision(18, 8);
            builder.Property(t => t.PriceChange).HasPrecision(18, 8);
            builder.Property(t => t.PriceChangePercent).HasPrecision(18, 8);
            builder.Property(t => t.TotalTrades);
            builder.Property(t => t.OpenTime);
            builder.Property(t => t.CloseTime);
            
            // Create an index on Symbol and Timestamp for faster queries
            builder.HasIndex(t => new { t.Symbol, t.Timestamp });
        }
    }
} 