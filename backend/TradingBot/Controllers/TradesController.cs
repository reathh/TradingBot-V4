using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Models;
using System.Linq.Expressions;

namespace TradingBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TradesController(TradingBotDbContext context) : ControllerBase
    {
        // GET: api/Trades
        [HttpGet]
        public async Task<ActionResult<PagedResult<TradeDto>>> GetTrades(
            [FromQuery] int? botId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Sanitize paging arguments
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Base query – only completed trades (exit filled)
            var baseQuery = context.Trades
                .Where(t => t.ExitOrder != null &&
                            t.ExitOrder.Status == OrderStatus.Filled &&
                            t.ExitOrder.QuantityFilled > 0);

            if (botId.HasValue)
            {
                baseQuery = baseQuery.Where(t => t.BotId == botId.Value);
            }

            var totalItems = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderByDescending(t => t.ExitOrder!.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TradeDto
                {
                    Id = t.Id,
                    BotId = t.BotId.ToString(),
                    Symbol = t.EntryOrder.Symbol,
                    EntryPrice = t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price,
                    ExitPrice = t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price,
                    Quantity = t.EntryOrder.Quantity,
                    QuantityFilled = t.EntryOrder.QuantityFilled,
                    EntryFee = t.EntryOrder.Fee,
                    ExitFee = t.ExitOrder.Fee,
                    IsLong = t.EntryOrder.IsBuy,
                    Profit = ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                              (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                             (t.EntryOrder.Fee + t.ExitOrder.Fee),
                    EntryTime = t.EntryOrder.CreatedAt,
                    ExitTime = t.ExitOrder.CreatedAt,
                    IsCompleted = t.ExitOrder.Status == OrderStatus.Filled
                })
                .ToListAsync();

            return new PagedResult<TradeDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        // GET: api/Trades/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TradeDto>> GetTrade(int id)
        {
            var trade = await context.Trades
                .Include(t => t.EntryOrder)
                .Include(t => t.ExitOrder)
                .Include(t => t.Bot)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trade == null)
            {
                return NotFound();
            }

            var tradeDto = new TradeDto
            {
                Id = trade.Id,
                BotId = trade.Bot?.Id.ToString() ?? "Unknown",
                Symbol = trade.EntryOrder.Symbol,
                EntryPrice = trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price,
                ExitPrice = trade.ExitOrder?.AverageFillPrice ?? trade.ExitOrder?.Price,
                Quantity = trade.EntryOrder.Quantity,
                QuantityFilled = trade.EntryOrder.QuantityFilled,
                EntryFee = trade.EntryOrder.Fee,
                ExitFee = trade.ExitOrder?.Fee ?? 0,
                IsLong = trade.EntryOrder.IsBuy,
                Profit = trade.ExitOrder is null ? null :
                    ((trade.ExitOrder.AverageFillPrice ?? trade.ExitOrder.Price) -
                    (trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price)) * trade.EntryOrder.Quantity -
                    (trade.EntryOrder.Fee + (trade.ExitOrder?.Fee ?? 0)),
                EntryTime = trade.EntryOrder.CreatedAt,
                ExitTime = trade.ExitOrder?.CreatedAt,
                IsCompleted = trade.ExitOrder?.Status == OrderStatus.Filled
            };

            return tradeDto;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardDto>> GetStats(
            [FromQuery] TimeInterval interval = TimeInterval.Day,
            [FromQuery] int? botId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            // Default date range
            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            var baseQuery = context.Trades
                .Where(t => t.ExitOrder != null &&
                            t.ExitOrder.Status == OrderStatus.Filled &&
                            t.ExitOrder.QuantityFilled > 0 &&
                            t.ExitOrder.CreatedAt >= startDate &&
                            t.ExitOrder.CreatedAt <= endDate);

            if (botId.HasValue)
            {
                baseQuery = baseQuery.Where(t => t.BotId == botId.Value);
            }

            IQueryable<ProfitAggregation> groupedQuery;
            TimeSpan bucket;

            switch (interval)
            {
                case TimeInterval.Minute:
                    groupedQuery = baseQuery
                        .GroupBy(t => new { t.ExitOrder!.CreatedAt.Year, t.ExitOrder.CreatedAt.Month, t.ExitOrder.CreatedAt.Day, t.ExitOrder.CreatedAt.Hour, t.ExitOrder.CreatedAt.Minute })
                        .Select(g => new ProfitAggregation
                        {
                            PeriodStart = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, g.Key.Minute, 0, DateTimeKind.Utc),
                            TotalProfit = Math.Round(
                                g.Sum(t => ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                            (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                            (t.EntryOrder.Fee + t.ExitOrder.Fee)), 8),
                            QuoteVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity *
                                           (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)), 8),
                            BaseVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity), 8),
                            TradeCount = g.Count(),
                            WinCount = g.Count(t => (((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                                   (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                                   (t.EntryOrder.Fee + t.ExitOrder.Fee)) > 0)
                        });
                    bucket = TimeSpan.FromMinutes(1);
                    break;
                case TimeInterval.Hour:
                    groupedQuery = baseQuery
                        .GroupBy(t => new { t.ExitOrder!.CreatedAt.Year, t.ExitOrder.CreatedAt.Month, t.ExitOrder.CreatedAt.Day, t.ExitOrder.CreatedAt.Hour })
                        .Select(g => new ProfitAggregation
                        {
                            PeriodStart = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0, DateTimeKind.Utc),
                            TotalProfit = Math.Round(
                                g.Sum(t => ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                            (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                            (t.EntryOrder.Fee + t.ExitOrder.Fee)), 8),
                            QuoteVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity *
                                           (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)), 8),
                            BaseVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity), 8),
                            TradeCount = g.Count(),
                            WinCount = g.Count(t => (((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                                   (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                                   (t.EntryOrder.Fee + t.ExitOrder.Fee)) > 0)
                        });
                    bucket = TimeSpan.FromHours(1);
                    break;
                case TimeInterval.Day:
                    groupedQuery = baseQuery
                        .GroupBy(t => new { t.ExitOrder!.CreatedAt.Year, t.ExitOrder.CreatedAt.Month, t.ExitOrder.CreatedAt.Day })
                        .Select(g => new ProfitAggregation
                        {
                            PeriodStart = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc),
                            TotalProfit = Math.Round(
                                g.Sum(t => ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                            (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                            (t.EntryOrder.Fee + t.ExitOrder.Fee)), 8),
                            QuoteVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity *
                                           (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)), 8),
                            BaseVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity), 8),
                            TradeCount = g.Count(),
                            WinCount = g.Count(t => (((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                                   (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                                   (t.EntryOrder.Fee + t.ExitOrder.Fee)) > 0)
                        });
                    bucket = TimeSpan.FromDays(1);
                    break;
                case TimeInterval.Week:
                    groupedQuery = baseQuery
                        .GroupBy(t => t.ExitOrder!.CreatedAt.Date.AddDays(-(int)t.ExitOrder.CreatedAt.DayOfWeek + (int)DayOfWeek.Monday))
                        .Select(g => new ProfitAggregation
                        {
                            PeriodStart = g.Key,
                            TotalProfit = Math.Round(
                                g.Sum(t => ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                            (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                            (t.EntryOrder.Fee + t.ExitOrder.Fee)), 8),
                            QuoteVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity *
                                           (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)), 8),
                            BaseVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity), 8),
                            TradeCount = g.Count(),
                            WinCount = g.Count(t => (((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                                   (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                                   (t.EntryOrder.Fee + t.ExitOrder.Fee)) > 0)
                        });
                    bucket = TimeSpan.FromDays(7);
                    break;
                case TimeInterval.Month:
                    groupedQuery = baseQuery
                        .GroupBy(t => new { t.ExitOrder!.CreatedAt.Year, t.ExitOrder.CreatedAt.Month })
                        .Select(g => new ProfitAggregation
                        {
                            PeriodStart = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                            TotalProfit = Math.Round(
                                g.Sum(t => ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                            (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                            (t.EntryOrder.Fee + t.ExitOrder.Fee)), 8),
                            QuoteVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity *
                                           (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)), 8),
                            BaseVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity), 8),
                            TradeCount = g.Count(),
                            WinCount = g.Count(t => (((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                                   (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                                   (t.EntryOrder.Fee + t.ExitOrder.Fee)) > 0)
                        });
                    bucket = TimeSpan.FromDays(30); // Approximate
                    break;
                case TimeInterval.Year:
                    groupedQuery = baseQuery
                        .GroupBy(t => t.ExitOrder!.CreatedAt.Year)
                        .Select(g => new ProfitAggregation
                        {
                            PeriodStart = new DateTime(g.Key, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            TotalProfit = Math.Round(
                                g.Sum(t => ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                            (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                            (t.EntryOrder.Fee + t.ExitOrder.Fee)), 8),
                            QuoteVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity *
                                           (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)), 8),
                            BaseVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity), 8),
                            TradeCount = g.Count(),
                            WinCount = g.Count(t => (((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                                   (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                                   (t.EntryOrder.Fee + t.ExitOrder.Fee)) > 0)
                        });
                    bucket = TimeSpan.FromDays(365); // Approximate
                    break;
                default:
                    groupedQuery = baseQuery
                        .GroupBy(t => new { t.ExitOrder!.CreatedAt.Year, t.ExitOrder.CreatedAt.Month, t.ExitOrder.CreatedAt.Day })
                        .Select(g => new ProfitAggregation
                        {
                            PeriodStart = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc),
                            TotalProfit = Math.Round(
                                g.Sum(t => ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                            (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                            (t.EntryOrder.Fee + t.ExitOrder.Fee)), 8),
                            QuoteVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity *
                                           (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)), 8),
                            BaseVolume = Math.Round(
                                g.Sum(t => t.EntryOrder.Quantity), 8),
                            TradeCount = g.Count(),
                            WinCount = g.Count(t => (((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                                   (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                                   (t.EntryOrder.Fee + t.ExitOrder.Fee)) > 0)
                        });
                    bucket = TimeSpan.FromDays(1);
                    break;
            }

            var groupedList = await groupedQuery.OrderBy(g => g.PeriodStart).ToListAsync();

            var statsSeries = groupedList.Select(g => new StatsDto
            {
                BotId = botId?.ToString(),
                TimePeriod = g.PeriodStart.ToString("yyyy-MM-dd HH:mm"),
                PeriodStart = g.PeriodStart,
                PeriodEnd = g.PeriodStart.Add(bucket).AddSeconds(-1),
                TotalProfit = g.TotalProfit,
                QuoteVolume = g.QuoteVolume,
                BaseVolume = g.BaseVolume,
                TradeCount = g.TradeCount,
                WinRate = g.TradeCount == 0 ? 0 : 100m * g.WinCount / g.TradeCount,
                ProfitPct = g.QuoteVolume == 0 ? 0 : Math.Round(100m * g.TotalProfit / g.QuoteVolume, 8)
            }).ToList();

            // Calculate last 24h summary metrics (based on EndDate reference)
            var last24hStart = endDate.Value.AddDays(-1);

            var lastDayAggregates = await baseQuery
                .Where(t => t.ExitOrder!.CreatedAt >= last24hStart && t.ExitOrder.CreatedAt <= endDate)
                .Select(t => new
                {
                    QuoteVolume = t.EntryOrder.Quantity * (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price),
                    BaseVolume = t.EntryOrder.Quantity,
                    Profit = ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                              (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                             (t.EntryOrder.Fee + t.ExitOrder.Fee)
                })
                .ToListAsync();

            decimal quoteVol24h = lastDayAggregates.Sum(a => a.QuoteVolume);
            decimal baseVol24h  = lastDayAggregates.Sum(a => a.BaseVolume);
            decimal roi24h      = lastDayAggregates.Sum(a =>
                                    a.QuoteVolume == 0 ? 0 : 100m * a.Profit / a.QuoteVolume);

            var dashboard = new DashboardDto
            {
                Roi24h = Math.Round(roi24h, 8),
                QuoteVolume24h = Math.Round(quoteVol24h, 8),
                BaseVolume24h = Math.Round(baseVol24h, 8),
                Stats = statsSeries
            };

            return dashboard;
        }

        // Nested type used for database projection of aggregated profit data
        private class ProfitAggregation
        {
            public DateTime PeriodStart { get; init; }
            public decimal TotalProfit { get; init; }
            public decimal QuoteVolume { get; init; }
            public decimal BaseVolume { get; init; }
            public int TradeCount { get; init; }
            public int WinCount { get; init; }
        }
    }

    public class TradeDto
    {
        public int Id { get; set; }
        public string BotId { get; set; } = null!;
        public string Symbol { get; set; } = null!;
        public decimal EntryPrice { get; set; }
        public decimal? ExitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityFilled { get; set; }
        public decimal EntryFee { get; set; }
        public decimal ExitFee { get; set; }
        public bool IsLong { get; set; }
        public decimal? Profit { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public bool IsCompleted { get; set; }
    }
}
