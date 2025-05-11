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
            [FromQuery] string? period = "month",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validate page parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = context.Trades
                .Include(t => t.EntryOrder)
                .Include(t => t.ExitOrder)
                .Include(t => t.Bot)
                .Where(t => t.ExitOrder != null && t.ExitOrder.Closed && t.ExitOrder.QuantityFilled > 0 && t.EntryOrder != null && t.ExitOrder != null);

            // Filter by bot ID if provided
            if (botId.HasValue)
            {
                query = query.Where(t => t.Bot != null && t.Bot.Id == botId.Value);
            }

            query = query.OrderByDescending(t => t.ExitOrder!.CreatedAt);

            // Get total count for pagination info
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var trades = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var tradeDtos = trades.Select(t => new TradeDto
            {
                Id = t.Id,
                BotId = t.Bot?.Id.ToString() ?? "Unknown",
                Symbol = t.EntryOrder.Symbol,
                EntryPrice = t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price,
                ExitPrice = t.ExitOrder?.AverageFillPrice ?? t.ExitOrder?.Price,
                Quantity = t.EntryOrder.Quantity,
                QuantityFilled = t.EntryOrder.QuantityFilled,
                EntryFee = t.EntryOrder.Fees,
                ExitFee = t.ExitOrder?.Fees ?? 0,
                IsLong = t.EntryOrder.IsBuy,
                Profit = CalculateProfit(t),
                EntryTime = t.EntryOrder.CreatedAt,
                ExitTime = t.ExitOrder?.CreatedAt,
                IsCompleted = t.ExitOrder != null && t.ExitOrder.Closed
            }).ToList();

            var result = new PagedResult<TradeDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalCount,
                Items = tradeDtos
            };

            return result;
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
                EntryFee = trade.EntryOrder.Fees,
                ExitFee = trade.ExitOrder?.Fees ?? 0,
                IsLong = trade.EntryOrder.IsBuy,
                Profit = trade.Profit,
                EntryTime = trade.EntryOrder.CreatedAt,
                ExitTime = trade.ExitOrder?.CreatedAt,
                IsCompleted = trade.ExitOrder != null && trade.ExitOrder.Closed
            };

            return tradeDto;
        }

        [HttpGet("profit-data")]
        public async Task<ActionResult<PagedResult<BotProfitDto>>> GetAggregatedProfits(
            [FromQuery] TimeInterval interval = TimeInterval.Day,
            [FromQuery] int? botId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Default date range if not specified
            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            // Set appropriate time range based on interval if not specified
            if (startDate == DateTime.UtcNow.AddMonths(-1))
            {
                startDate = interval switch
                {
                    TimeInterval.Minute => DateTime.UtcNow.AddHours(-1),
                    TimeInterval.Hour => DateTime.UtcNow.AddDays(-1),
                    TimeInterval.Day => DateTime.UtcNow.AddDays(-7),
                    TimeInterval.Week => DateTime.UtcNow.AddMonths(-1),
                    TimeInterval.Month => DateTime.UtcNow.AddMonths(-6),
                    TimeInterval.Year => DateTime.UtcNow.AddYears(-1),
                    _ => DateTime.UtcNow.AddMonths(-1)
                };
            }

            // Get trades within the date range
            var query = context.Trades
                .Include(t => t.EntryOrder)
                .Include(t => t.ExitOrder)
                .Include(t => t.Bot)
                .Where(t => t.ExitOrder != null && t.ExitOrder.Closed && t.ExitOrder.QuantityFilled > 0)
                .Where(t => t.ExitOrder!.CreatedAt >= startDate && t.ExitOrder.CreatedAt <= endDate);

            // Filter by bot if specified
            if (botId.HasValue)
            {
                query = query.Where(t => t.Bot.Id == botId.Value);
            }

            // Convert to list for in-memory grouping
            var trades = await query.ToListAsync();

            // Group trades by the specified time interval
            var groupedTrades = trades
                .GroupBy(t => GetPeriodGroup(t.ExitOrder!.CreatedAt, interval))
                .Select(g => new
                {
                    TimePeriod = g.Key,
                    PeriodStart = GetPeriodStart(g.First().ExitOrder!.CreatedAt, interval),
                    PeriodEnd = GetPeriodEnd(g.First().ExitOrder!.CreatedAt, interval),
                    BotId = botId.HasValue ? botId.Value.ToString() : null,
                    TotalProfit = g.Sum(t => t.Profit ?? CalculateProfit(t)),
                    TotalVolume = g.Sum(t => t.EntryOrder!.Quantity * (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)),
                    TradeCount = g.Count(),
                    WinRate = g.Count(t => (t.Profit ?? 0) > 0) * 100.0m / g.Count(),
                })
                .OrderByDescending(g => g.PeriodStart)
                .ToList();

            // Calculate total count of periods
            var totalCount = groupedTrades.Count;

            // Apply pagination
            var pagedGroups = groupedTrades
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Convert to DTOs
            var botProfitDtos = pagedGroups.Select(g => new BotProfitDto
            {
                BotId = g.BotId,
                TimePeriod = g.TimePeriod,
                TotalProfit = g.TotalProfit,
                TotalVolume = g.TotalVolume,
                TradeCount = g.TradeCount,
                WinRate = g.WinRate,
                PeriodStart = g.PeriodStart,
                PeriodEnd = g.PeriodEnd
            }).ToList();

            return new PagedResult<BotProfitDto>
            {
                Items = botProfitDtos,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        private string GetPeriodGroup(DateTime date, TimeInterval interval)
        {
            return interval switch
            {
                TimeInterval.Minute => date.ToString("yyyy-MM-dd HH:mm"),
                TimeInterval.Hour => date.ToString("yyyy-MM-dd HH"),
                TimeInterval.Day => date.ToString("yyyy-MM-dd"),
                TimeInterval.Week => $"{date.Year}-W{GetIsoWeekNumber(date)}",
                TimeInterval.Month => date.ToString("yyyy-MM"),
                TimeInterval.Year => date.ToString("yyyy"),
                _ => date.ToString("yyyy-MM-dd")
            };
        }

        private DateTime GetPeriodStart(DateTime date, TimeInterval interval)
        {
            return interval switch
            {
                TimeInterval.Minute => new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, DateTimeKind.Utc),
                TimeInterval.Hour => new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0, DateTimeKind.Utc),
                TimeInterval.Day => date.Date,
                TimeInterval.Week => GetStartOfWeek(date),
                TimeInterval.Month => new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                TimeInterval.Year => new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                _ => date.Date
            };
        }

        private DateTime GetPeriodEnd(DateTime date, TimeInterval interval)
        {
            return interval switch
            {
                TimeInterval.Minute => GetPeriodStart(date, interval).AddMinutes(1).AddSeconds(-1),
                TimeInterval.Hour => GetPeriodStart(date, interval).AddHours(1).AddSeconds(-1),
                TimeInterval.Day => GetPeriodStart(date, interval).AddDays(1).AddSeconds(-1),
                TimeInterval.Week => GetPeriodStart(date, interval).AddDays(7).AddSeconds(-1),
                TimeInterval.Month => GetPeriodStart(date, interval).AddMonths(1).AddSeconds(-1),
                TimeInterval.Year => GetPeriodStart(date, interval).AddYears(1).AddSeconds(-1),
                _ => GetPeriodStart(date, interval).AddDays(1).AddSeconds(-1)
            };
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private int GetIsoWeekNumber(DateTime date)
        {
            var day = (int)System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date.AddDays(4 - (day == 0 ? 7 : day)),
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
        }

        private decimal CalculateProfit(Trade trade)
        {
            if (trade.EntryOrder == null || trade.ExitOrder == null)
                return 0;

            decimal entryPrice = trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price;
            decimal exitPrice = trade.ExitOrder.AverageFillPrice ?? trade.ExitOrder.Price;
            decimal quantity = trade.EntryOrder.Quantity;
            decimal entryFee = trade.EntryOrder.Fees;
            decimal exitFee = trade.ExitOrder.Fees;

            // Calculate profit as (exit price - entry price) * quantity - fees
            return (exitPrice - entryPrice) * quantity - (entryFee + exitFee);
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
