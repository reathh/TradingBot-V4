using MediatR;
using TradingBot.Data;

namespace TradingBot.Application;

public class NewTckerCommand : IRequest
{
    public required Ticker Ticker { get; set; }

    public class NewTckerCommandHandler(TradingBotDbContext db) : IRequestHandler<NewTckerCommand>
    {
        private TradingBotDbContext db = db;

        public async Task Handle(NewTckerCommand request, CancellationToken cancellationToken)
        {
            var bots = await db.Bots
            .Where(b => b.Enabled)

        }
    }