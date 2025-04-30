using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;

namespace TradingBot.Application.Commands.PlaceOpeningOrders;


public class PlaceOpeningOrderCommand : IRequest
{
    public class PlaceOpeningOrderCommandHandler(TradingBotDbContext db) : IRequestHandler<PlaceOpeningOrderCommand>
    {
        public async Task Handle(PlaceOpeningOrderCommand request, CancellationToken cancellationToken)
        {
            var bot = await db.Bots
            .Where(b => b.Enabled)
            .Where(b => b
            .Strategies
            .Where(s => s.Enabled)
            .Where(s => s.Trades.Count == 0
            || s
            .Trades
            .Where(t => t.Profit is null))
            .All())
            .FirstOrDefaultAsync(cancellationToken);

            if (bot is null)
            {
                return;
            }
        }
    }
}