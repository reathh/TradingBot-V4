using MediatR;
using TradingBot.Data;
using Microsoft.EntityFrameworkCore;

namespace TradingBot.Application;

public class NewTickerCommand : IRequest
{
    public required Ticker Ticker { get; set; }

    public class NewTickerCommandHandler(TradingBotDbContext db) : IRequestHandler<NewTickerCommand>
    {
        private readonly TradingBotDbContext db = db;

        public async Task Handle(NewTickerCommand request, CancellationToken cancellationToken)
        {
            var bots = await db.Bots
                .Where(b => b.Enabled)
                .ToListAsync(cancellationToken);
        }
    }
}