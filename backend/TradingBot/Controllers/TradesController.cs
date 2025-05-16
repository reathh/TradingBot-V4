using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Models;

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
                            t.ExitOrder.Status == OrderStatus.Filled);

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
                            AvailableCapital = Math.Round(
                                g.Select(t => t.AvailableCapital).FirstOrDefault(), 8),
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
                            AvailableCapital = Math.Round(
                                g.Select(t => t.AvailableCapital).FirstOrDefault(), 8),
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
                            AvailableCapital = Math.Round(
                                g.Select(t => t.AvailableCapital).FirstOrDefault(), 8),
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
                            AvailableCapital = Math.Round(
                                g.Select(t => t.AvailableCapital).FirstOrDefault(), 8),
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
                            AvailableCapital = Math.Round(
                                g.Select(t => t.AvailableCapital).FirstOrDefault(), 8),
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
                            AvailableCapital = Math.Round(
                                g.Select(t => t.AvailableCapital).FirstOrDefault(), 8),
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
                            AvailableCapital = Math.Round(
                                g.Select(t => t.AvailableCapital).FirstOrDefault(), 8),
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

            // Fill in missing periods with zero values
            var completeStatsList = FillMissingPeriods(groupedList, startDate.Value, endDate.Value, bucket);

            var statsSeries = completeStatsList.Select(g => new StatsDto
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
                ProfitPct = g.AvailableCapital == 0 ? 0 : Math.Round(100m * g.TotalProfit / g.AvailableCapital, 8)
            }).ToList();

            // Calculate last 24h summary metrics (based on EndDate reference)
            var last24hStart = endDate.Value.AddDays(-1);

            var lastDayAggregates = await baseQuery
                .Where(t => t.ExitOrder!.CreatedAt >= last24hStart && t.ExitOrder.CreatedAt <= endDate)
                .Select(t => new
                {
                    QuoteVolume = t.EntryOrder.Quantity * (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price),
                    BaseVolume = t.EntryOrder.Quantity,
                    AvailableCapital = t.AvailableCapital,
                    ExitTime = t.ExitOrder!.CreatedAt,
                    Profit = ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                              (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                             (t.EntryOrder.Fee + t.ExitOrder.Fee)
                })
                .ToListAsync();

            decimal quoteVol24h = lastDayAggregates.Sum(a => a.QuoteVolume);
            decimal baseVol24h  = lastDayAggregates.Sum(a => a.BaseVolume);
            decimal availableCapital24h = lastDayAggregates.Any() ? lastDayAggregates.OrderByDescending(a => a.ExitTime).First().AvailableCapital : 0m;
            decimal profit24h = lastDayAggregates.Sum(a => a.Profit);
            decimal roi24h = availableCapital24h == 0 ? 0 : 100m * profit24h / availableCapital24h;

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
            public decimal TotalProfit { get; init; } = 0;
            public decimal QuoteVolume { get; init; } = 0;
            public decimal AvailableCapital { get; init; } = 0;
            public decimal BaseVolume { get; init; } = 0;
            public int TradeCount { get; init; } = 0;
            public int WinCount { get; init; } = 0;
        }

        // Helper method to fill in missing periods with zero values
        private List<ProfitAggregation> FillMissingPeriods(
            List<ProfitAggregation> existingData,
            DateTime startDate,
            DateTime endDate,
            TimeSpan interval)
        {
            var result = new List<ProfitAggregation>();
            var current = NormalizeDate(startDate, interval);
            var normalizedEnd = NormalizeDate(endDate, interval);

            // Create a dictionary of existing data points by period start time
            var existingByPeriod = existingData.ToDictionary(
                g => g.PeriodStart,
                g => g
            );

            // Generate all periods in the range
            while (current <= normalizedEnd)
            {
                if (existingByPeriod.TryGetValue(current, out var existingPeriod))
                {
                    // Use existing data if available
                    result.Add(existingPeriod);
                }
                else
                {
                    // Create a zero-value entry for this period
                    result.Add(new ProfitAggregation
                    {
                        PeriodStart = current,
                        TotalProfit = 0,
                        QuoteVolume = 0,
                        AvailableCapital = existingData.Any() ? existingData.OrderByDescending(e => e.PeriodStart).First().AvailableCapital : 0,
                        BaseVolume = 0,
                        TradeCount = 0,
                        WinCount = 0
                    });
                }

                current = current.Add(interval);
            }

            return result;
        }

        // Helper method to normalize a date to the start of its interval
        private DateTime NormalizeDate(DateTime date, TimeSpan interval)
        {
            if (interval.TotalMinutes == 1)
            {
                return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, DateTimeKind.Utc);
            }
            else if (interval.TotalHours == 1)
            {
                return new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0, DateTimeKind.Utc);
            }
            else if (interval.TotalDays == 1)
            {
                return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else if (interval.TotalDays == 7)
            {
                // For weekly, normalize to the Monday of the week
                var dayOfWeek = (int)date.DayOfWeek;
                var daysToSubtract = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Monday is 1
                return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-daysToSubtract);
            }
            else if (interval.TotalDays >= 28 && interval.TotalDays <= 31)
            {
                // For monthly, normalize to the 1st of the month
                return new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            else if (interval.TotalDays >= 365)
            {
                // For yearly, normalize to January 1st
                return new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            
            // Default normalization
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0, DateTimeKind.Utc);
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
