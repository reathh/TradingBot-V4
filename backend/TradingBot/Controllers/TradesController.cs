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
    public class TradesController : ControllerBase
    {
        private readonly TradingBotDbContext _context;

        public TradesController(TradingBotDbContext context)
        {
            _context = context;
        }

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

            var tradeDtos = trades.Select(t => new TradeDto
            {
                TradeId = t.Id,
                BotId = t.Bot?.Id.ToString() ?? "Unknown",
                Symbol = t.EntryOrder.Symbol,
                EntryPrice = t.EntryOrder.Price,
                ExitPrice = t.ExitOrder.Price,
                EntryAvgPrice = t.EntryOrder.AverageFillPrice ?? t.EntryOrder.Price,
                ExitAvgPrice = t.ExitOrder.AverageFillPrice ?? t.ExitOrder.Price,
                Quantity = t.EntryOrder.Quantity,
                QuantityFilled = t.EntryOrder.QuantityFilled,
                EntryFee = t.EntryOrder.Fees,
                ExitFee = t.ExitOrder.Fees,
                IsLong = t.EntryOrder.IsBuy,
                Profit = CalculateProfit(t),
                EntryTime = t.EntryOrder.CreatedAt,
                ExitTime = t.ExitOrder.CreatedAt,
                IsCompleted = t.ExitOrder != null && t.ExitOrder.Closed
            }).ToList();

            var result = new PagedResult<TradeDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalCount = totalCount,
                Items = tradeDtos
            };

            return result;
        }

        // GET: api/Trades/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TradeDto>> GetTrade(int id)
        {
            var trade = await _context.Trades
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
                TradeId = trade.Id,
                BotId = trade.Bot?.Id.ToString() ?? "Unknown",
                Symbol = trade.EntryOrder.Symbol,
                EntryPrice = trade.EntryOrder.Price,
                ExitPrice = trade.ExitOrder?.Price ?? 0,
                EntryAvgPrice = trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price,
                ExitAvgPrice = trade.ExitOrder?.AverageFillPrice ?? trade.ExitOrder?.Price ?? 0,
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

        private decimal CalculateProfit(Trade trade)
        {
            if (trade.ExitOrder == null || trade.EntryOrder == null)
            {
                return 0;
            }

            var entryPrice = trade.EntryOrder.AverageFillPrice ?? trade.EntryOrder.Price;
            var exitPrice = trade.ExitOrder.AverageFillPrice ?? trade.ExitOrder.Price;
            var quantity = trade.EntryOrder.QuantityFilled;

            if (trade.EntryOrder.IsBuy)
            {
                // Long position
                return (exitPrice - entryPrice) * quantity - trade.EntryOrder.Fees - trade.ExitOrder.Fees;
            }
            else
            {
                // Short position
                return (entryPrice - exitPrice) * quantity - trade.EntryOrder.Fees - trade.ExitOrder.Fees;
            }
        }
    }

    public class TradeDto
    {
        public int TradeId { get; set; }
        public string BotId { get; set; } = null!;
        public string Symbol { get; set; } = null!;
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal EntryAvgPrice { get; set; }
        public decimal ExitAvgPrice { get; set; }
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
