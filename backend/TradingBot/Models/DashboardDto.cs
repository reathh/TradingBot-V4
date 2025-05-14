namespace TradingBot.Models
{
    using System;
    public class DashboardDto
    {
        public decimal Roi24h { get; init; }
        public decimal QuoteVolume24h { get; init; }
        public decimal BaseVolume24h { get; init; }

        public IEnumerable<StatsDto> Stats { get; init; } = Array.Empty<StatsDto>();
    }
} 