using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Models;

namespace TradingBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly TradingBotDbContext _context;

        public DashboardController(TradingBotDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummary>> GetSummary()
        {
            var bots = await _context.Bots.CountAsync();
            var activeOrders = await _context.Orders.CountAsync(o => !o.Closed && !o.Canceled);

            var completedTrades = await _context.Trades.CountAsync(t => t.ExitOrder != null && t.ExitOrder.Closed && t.ExitOrder.QuantityFilled > 0);
            var totalProfit = await _context.Trades
                .Where(t => t.ExitOrder != null && t.ExitOrder.Closed && t.ExitOrder.QuantityFilled > 0)
                .SumAsync(t => t.Profit ?? 0);

            return new DashboardSummary
            {
                TotalBots = bots,
                ActiveOrders = activeOrders,
                CompletedTrades = completedTrades,
                TotalProfit = totalProfit
            };
        }

        [HttpGet("recent-trades")]
        public async Task<ActionResult<IEnumerable<TradeDto>>> GetRecentTrades()
        {
            return await _context.Trades
                .Include(t => t.EntryOrder)
                .Include(t => t.ExitOrder)
                .Where(t => t.EntryOrder != null)
                .OrderByDescending(t => t.EntryOrder.CreatedAt)
                .Take(5)
                .Select(t => new TradeDto
                {
                    Symbol = t.EntryOrder.Symbol,
                    Direction = t.EntryOrder.IsBuy ? "Buy" : "Sell",
                    EntryPrice = t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price,
                    ExitPrice = t.ExitOrder != null ? (t.ExitOrder.AverageFillPrice ?? t.ExitOrder.Price) : 0,
                    ProfitLoss = t.Profit ?? 0,
                    Timestamp = t.EntryOrder.CreatedAt,
                    Status = (t.ExitOrder != null && t.ExitOrder.Closed && t.ExitOrder.QuantityFilled > 0) ? "Completed" : "Open"
                })
                .ToListAsync();
        }

        [HttpGet("performance")]
        public async Task<ActionResult<PerformanceData>> GetPerformance()
        {
            var today = DateTime.UtcNow.Date;
            var lastYear = today.AddYears(-1);

            var monthlyPerformance = await _context.Trades
                .Include(t => t.EntryOrder)
                .Where(t => t.EntryOrder.CreatedAt >= lastYear && t.ExitOrder != null && t.ExitOrder.Closed && t.ExitOrder.QuantityFilled > 0)
                .GroupBy(t => new { t.EntryOrder.CreatedAt.Year, t.EntryOrder.CreatedAt.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    ProfitLoss = g.Sum(t => t.Profit ?? 0)
                })
                .ToListAsync();

            var months = new string[] { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
            var profitData = new decimal[12];

            foreach (var point in monthlyPerformance)
            {
                profitData[point.Month.Month - 1] = point.ProfitLoss;
            }

            return new PerformanceData
            {
                Labels = months,
                Data = profitData
            };
        }

        [HttpGet("bot-profits")]
        public async Task<ActionResult<PagedResult<BotProfitDto>>> GetBotProfits(
            [FromQuery] string? period = "month",
            [FromQuery] int? botId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validate page parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.Trades
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

            var botProfits = trades.Select(t => new BotProfitDto
            {
                BotId = t.Bot?.Id.ToString() ?? "Unknown",
                Ticker = t.EntryOrder.Symbol,
                EntryAvgPrice = t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price,
                ExitAvgPrice = t.ExitOrder.AverageFillPrice ?? t.ExitOrder.Price,
                Quantity = t.EntryOrder.Quantity,
                EntryFee = t.EntryOrder.Fees,
                ExitFee = t.ExitOrder.Fees,
                Profit = CalculateProfit(t),
                EntryTime = t.EntryOrder.CreatedAt,
                ExitTime = t.ExitOrder.CreatedAt
            }).ToList();

            var result = new PagedResult<BotProfitDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalCount = totalCount,
                Items = botProfits
            };

            return result;
        }

        [HttpGet("bot-profits-chart")]
        public async Task<ActionResult<ProfitChartData>> GetBotProfitsChart([FromQuery] string period = "month")
        {
            DateTime startDate;
            string[] labels;
            Func<DateTime, string> getGroupKey;

            // Set time range and grouping based on period
            switch (period.ToLower())
            {
                case "day":
                    startDate = DateTime.UtcNow.Date.AddDays(-1);
                    labels = Enumerable.Range(0, 24).Select(h => $"{h:D2}:00").ToArray();
                    getGroupKey = (date) => date.Hour.ToString("D2") + ":00";
                    break;
                case "week":
                    startDate = DateTime.UtcNow.Date.AddDays(-7);
                    labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
                    getGroupKey = (date) => labels[((int)date.DayOfWeek + 6) % 7]; // Adjusting for Mon=0, Sun=6
                    break;
                case "year":
                    startDate = DateTime.UtcNow.Date.AddYears(-1);
                    labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                    getGroupKey = (date) => labels[date.Month - 1];
                    break;
                case "month":
                default:
                    startDate = DateTime.UtcNow.Date.AddMonths(-1);
                    // Create labels for 4 weeks
                    labels = new[] { "Week 1", "Week 2", "Week 3", "Week 4", "Week 5" };
                    getGroupKey = (date) =>
                    {
                        int weekNum = (int)Math.Ceiling((double)date.Day / 7);
                        return $"Week {weekNum}";
                    };
                    break;
            }

            // Get completed trades within the time period
            var trades = await _context.Trades
                .Include(t => t.EntryOrder)
                .Include(t => t.ExitOrder)
                .Where(t => t.ExitOrder != null && t.ExitOrder.Closed && t.ExitOrder.QuantityFilled > 0 && t.ExitOrder.CreatedAt >= startDate)
                .ToListAsync();

            // Group by the period and sum the profits
            var groupedProfits = trades
                .GroupBy(t => getGroupKey(t.ExitOrder.CreatedAt))
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Profit ?? CalculateProfit(t)));

            // Prepare data array to match the labels
            var data = new decimal[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                if (groupedProfits.TryGetValue(labels[i], out decimal profit))
                {
                    data[i] = profit;
                }
            }

            return new ProfitChartData
            {
                Labels = labels,
                Data = data
            };
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

    public class DashboardSummary
    {
        public int TotalBots { get; set; }
        public int ActiveOrders { get; set; }
        public int CompletedTrades { get; set; }
        public decimal TotalProfit { get; set; }
    }

    public class TradeDto
    {
        public required string Symbol { get; set; }
        public required string Direction { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal ProfitLoss { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Status { get; set; }
    }

    public class PerformanceData
    {
        public required string[] Labels { get; set; }
        public required decimal[] Data { get; set; }
    }

    public class TaskItem
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public bool Done { get; set; }
    }

    public class BotProfitDto
    {
        public required string BotId { get; set; }
        public required string Ticker { get; set; }
        public decimal EntryAvgPrice { get; set; }
        public decimal ExitAvgPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal EntryFee { get; set; }
        public decimal ExitFee { get; set; }
        public decimal Profit { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
    }

    public class ProfitChartData
    {
        public required string[] Labels { get; set; }
        public required decimal[] Data { get; set; }
    }
}