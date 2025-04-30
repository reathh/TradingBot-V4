using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TradingBot.Data
{
    public class BotConfiguration : IEntityTypeConfiguration<Bot>
    {
        public void Configure(EntityTypeBuilder<Bot> builder)
        {
        }
    }
}
