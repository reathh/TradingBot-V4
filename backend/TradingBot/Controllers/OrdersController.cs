using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Models;

namespace TradingBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly TradingBotDbContext _context;

        public OrdersController(TradingBotDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<OrderDto>>> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? botId = null,
            [FromQuery] string? period = null,
            [FromQuery] string? searchQuery = null,
            [FromQuery] string? sortKey = null,
            [FromQuery] string? sortDirection = null)
        {
            // Start with the base query
            var query = _context.Orders.AsQueryable();

            // Apply botId filter if provided
            if (botId.HasValue)
            {
                // Get all trades related to this bot
                var botTrades = await _context.Trades
                    .Where(t => t.BotId == botId)
                    .Select(t => new { EntryOrderId = t.EntryOrder.Id, ExitOrderId = t.ExitOrder != null ? t.ExitOrder.Id : null })
                    .ToListAsync();

                // Get all order IDs related to those trades
                var orderIds = botTrades
                    .SelectMany(t => new[] { t.EntryOrderId, t.ExitOrderId })
                    .Where(id => id != null)
                    .ToList();

                // Filter orders by those IDs
                query = query.Where(o => orderIds.Contains(o.Id));
            }

            // Apply search query if provided
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.ToLower();
                query = query.Where(o =>
                    o.Id.ToLower().Contains(searchQuery) ||
                    o.Symbol.ToLower().Contains(searchQuery));
            }

            // Apply time period filter if provided
            if (!string.IsNullOrWhiteSpace(period))
            {
                var now = DateTime.UtcNow;
                var startDate = period switch
                {
                    "day" => now.AddDays(-1),
                    "week" => now.AddDays(-7),
                    "month" => now.AddMonths(-1),
                    "year" => now.AddYears(-1),
                    _ => DateTime.MinValue
                };

                query = query.Where(o => o.CreatedAt >= startDate);
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(sortKey))
            {
                // Define sorting based on the sortKey
                query = (sortKey.ToLower(), sortDirection?.ToLower()) switch
                {
                    ("id", "desc") => query.OrderByDescending(o => o.Id),
                    ("id", _) => query.OrderBy(o => o.Id),
                    ("symbol", "desc") => query.OrderByDescending(o => o.Symbol),
                    ("symbol", _) => query.OrderBy(o => o.Symbol),
                    ("price", "desc") => query.OrderByDescending(o => o.Price),
                    ("price", _) => query.OrderBy(o => o.Price),
                    ("quantity", "desc") => query.OrderByDescending(o => o.Quantity),
                    ("quantity", _) => query.OrderBy(o => o.Quantity),
                    ("createdat", "desc") => query.OrderByDescending(o => o.CreatedAt),
                    ("createdat", _) => query.OrderBy(o => o.CreatedAt),
                    _ => query.OrderByDescending(o => o.CreatedAt) // Default sorting
                };
            }
            else
            {
                // Default sorting by created date
                query = query.OrderByDescending(o => o.CreatedAt);
            }

            // Count total items for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                Symbol = o.Symbol,
                Price = o.Price,
                AverageFillPrice = o.AverageFillPrice,
                Quantity = o.Quantity,
                QuantityFilled = o.QuantityFilled,
                IsBuy = o.IsBuy,
                Fee = o.Fee,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                LastUpdated = o.LastUpdated
            }).ToList();

            return new PagedResult<OrderDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Items = orderDtos
            };
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(string id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return new OrderDto
            {
                Id = order.Id,
                Symbol = order.Symbol,
                Price = order.Price,
                AverageFillPrice = order.AverageFillPrice,
                Quantity = order.Quantity,
                QuantityFilled = order.QuantityFilled,
                IsBuy = order.IsBuy,
                Fee = order.Fee,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                LastUpdated = order.LastUpdated
            };
        }
    }

    public class OrderDto
    {
        public string Id { get; set; } = null!;
        public string Symbol { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? AverageFillPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityFilled { get; set; }
        public bool IsBuy { get; set; }
        public decimal Fee { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}