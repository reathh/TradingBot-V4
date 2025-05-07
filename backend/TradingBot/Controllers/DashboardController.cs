using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradingBot.Data;

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
            var completedTrades = await _context.Trades.CountAsync(t => t.IsCompleted);
            var totalProfit = await _context.Trades
                .Where(t => t.IsCompleted)
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
                    Status = t.IsCompleted ? "Completed" : "Open"
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
                .Where(t => t.EntryOrder.CreatedAt >= lastYear && t.IsCompleted)
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

        [HttpGet("tasks")]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            // In a real application, these would come from a database
            // For now, returning sample tasks
            var tasks = new List<TaskItem>
            {
                new TaskItem { Title = "Update Bot Configuration", Description = "Adjust parameters for BTC/USD pair", Done = false },
                new TaskItem { Title = "Review Trade Performance", Description = "Analyze last week's trading results", Done = true },
                new TaskItem { Title = "Fix API Connection Issue", Description = "Troubleshoot connection drops with exchange API", Done = false },
                new TaskItem { Title = "Deploy New Strategy", Description = "Implement the moving average crossover strategy", Done = false },
                new TaskItem { Title = "Backup Database", Description = "Create weekly backup of trading history", Done = false }
            };

            return tasks;
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
        public string Symbol { get; set; }
        public string Direction { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal ProfitLoss { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }

    public class PerformanceData
    {
        public string[] Labels { get; set; }
        public decimal[] Data { get; set; }
    }

    public class TaskItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Done { get; set; }
    }
}