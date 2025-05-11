using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Models;

namespace TradingBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotsController(TradingBotDbContext context) : ControllerBase
    {
        // GET: api/Bots
        [HttpGet]
        public async Task<ActionResult<PagedResult<Bot>>> GetBots(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = context.Bots.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(search) ||
                                         b.Symbol.ToLower().Contains(search));
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var bots = await query
                .OrderBy(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create PagedResult
            var result = new PagedResult<Bot>
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalCount,
                Items = bots
            };

            return result;
        }

        // GET: api/Bots/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Bot>> GetBot(int id)
        {
            var bot = await context.Bots.FindAsync(id);

            if (bot == null)
            {
                return NotFound();
            }

            return bot;
        }

        // PUT: api/Bots/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBot(int id, Bot bot)
        {
            if (id != bot.Id)
            {
                return BadRequest();
            }

            context.Entry(bot).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) when (!BotExists(id))
            {
                return NotFound();
            }

            return NoContent();
        }

        // POST: api/Bots
        [HttpPost]
        public async Task<ActionResult<Bot>> CreateBot(Bot bot)
        {
            context.Bots.Add(bot);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBot), new { id = bot.Id }, bot);
        }

        // DELETE: api/Bots/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBot(int id)
        {
            var bot = await context.Bots.FindAsync(id);
            if (bot == null)
            {
                return NotFound();
            }

            context.Bots.Remove(bot);
            await context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Bots/5/toggle
        [HttpPost("{id}/toggle")]
        public async Task<IActionResult> ToggleBotStatus(int id)
        {
            var bot = await context.Bots.FindAsync(id);
            if (bot == null)
            {
                return NotFound();
            }

            bot.Enabled = !bot.Enabled;
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool BotExists(int id)
        {
            return context.Bots.Any(e => e.Id == id);
        }
    }

    public class BotTradeDto
    {
        public int TradeId { get; set; }
        public string EntryOrderId { get; set; } = null!;
        public string? ExitOrderId { get; set; }
        public string Symbol { get; set; } = null!;
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal? EntryAvgFillPrice { get; set; }
        public decimal? ExitAvgFillPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityFilled { get; set; }
        public bool IsLong { get; set; }
        public decimal? Profit { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public bool IsCompleted { get; set; }
    }
}