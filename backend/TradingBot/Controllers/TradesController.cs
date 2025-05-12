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
                    EntryFee = t.EntryOrder.Fees,
                    ExitFee = t.ExitOrder.Fees,
                    IsLong = t.EntryOrder.IsBuy,
                    Profit = ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                              (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                             (t.EntryOrder.Fees + t.ExitOrder.Fees),
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
                EntryFee = trade.EntryOrder.Fees,
                ExitFee = trade.ExitOrder?.Fees ?? 0,
                IsLong = trade.EntryOrder.IsBuy,
                Profit = trade.ExitOrder is null ? null :
                    ((trade.ExitOrder.AverageFillPrice ?? trade.ExitOrder.Price) -
                    (trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price)) * trade.EntryOrder.Quantity -
                    (trade.EntryOrder.Fees + (trade.ExitOrder?.Fees ?? 0)),
                EntryTime = trade.EntryOrder.CreatedAt,
                ExitTime = trade.ExitOrder?.CreatedAt,
                IsCompleted = trade.ExitOrder?.Status == OrderStatus.Filled
            };

            return tradeDto;
        }

        [HttpGet("profit-data")]
        public async Task<ActionResult<IEnumerable<BotProfitDto>>> GetAggregatedProfits(
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

            Expression<Func<Trade, DateTime>> keySelector;
            TimeSpan bucket;

            switch (interval)
            {
                case TimeInterval.Minute:
                    keySelector = t => new DateTime(t.ExitOrder!.CreatedAt.Year, t.ExitOrder.CreatedAt.Month, t.ExitOrder.CreatedAt.Day, t.ExitOrder.CreatedAt.Hour, t.ExitOrder.CreatedAt.Minute, 0, DateTimeKind.Utc);
                    bucket = TimeSpan.FromMinutes(1);
                    break;
                case TimeInterval.Hour:
                    keySelector = t => new DateTime(t.ExitOrder!.CreatedAt.Year, t.ExitOrder.CreatedAt.Month, t.ExitOrder.CreatedAt.Day, t.ExitOrder.CreatedAt.Hour, 0, 0, DateTimeKind.Utc);
                    bucket = TimeSpan.FromHours(1);
                    break;
                case TimeInterval.Day:
                    keySelector = t => t.ExitOrder!.CreatedAt.Date;
                    bucket = TimeSpan.FromDays(1);
                    break;
                case TimeInterval.Week:
                    keySelector = t => t.ExitOrder!.CreatedAt.Date.AddDays(-(int)t.ExitOrder.CreatedAt.DayOfWeek + (int)DayOfWeek.Monday);
                    bucket = TimeSpan.FromDays(7);
                    break;
                case TimeInterval.Month:
                    keySelector = t => new DateTime(t.ExitOrder!.CreatedAt.Year, t.ExitOrder.CreatedAt.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    bucket = TimeSpan.FromDays(30);
                    break;
                case TimeInterval.Year:
                    keySelector = t => new DateTime(t.ExitOrder!.CreatedAt.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    bucket = TimeSpan.FromDays(365);
                    break;
                default:
                    keySelector = t => t.ExitOrder!.CreatedAt.Date;
                    bucket = TimeSpan.FromDays(1);
                    break;
            }

            var grouped = await baseQuery
                .GroupBy(keySelector)
                .Select(g => new
                {
                    PeriodStart = g.Key,
                    TotalProfit = g.Sum(t => ((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                         (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                         (t.EntryOrder.Fees + t.ExitOrder.Fees)),
                    TotalVolume = g.Sum(t => t.EntryOrder.Quantity *
                                         (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)),
                    TradeCount = g.Count(),
                    WinCount = g.Count(t => (((t.ExitOrder!.AverageFillPrice ?? t.ExitOrder.Price) -
                                               (t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price)) * t.EntryOrder.Quantity -
                                               (t.EntryOrder.Fees + t.ExitOrder.Fees)) > 0)
                })
                .OrderBy(g => g.PeriodStart)
                .ToListAsync();

            var result = grouped.Select(g => new BotProfitDto
            {
                BotId = botId?.ToString(),
                TimePeriod = g.PeriodStart.ToString("yyyy-MM-dd HH:mm"),
                PeriodStart = g.PeriodStart,
                PeriodEnd = g.PeriodStart.Add(bucket).AddSeconds(-1),
                TotalProfit = g.TotalProfit,
                TotalVolume = g.TotalVolume,
                TradeCount = g.TradeCount,
                WinRate = g.TradeCount == 0 ? 0 : 100m * g.WinCount / g.TradeCount
            }).ToList();

            return result;
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
