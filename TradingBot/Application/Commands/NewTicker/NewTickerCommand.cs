using MediatR;
using TradingBot.Data;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Common;

namespace TradingBot.Application;

public class NewTickerCommand : IRequest<Result>
{
    public required Ticker Ticker { get; set; }

    public class NewTickerCommandHandler(TradingBotDbContext db, ILogger<NewTickerCommandHandler> logger) : BaseCommandHandler<NewTickerCommand>(logger)
    {
        private readonly TradingBotDbContext _db = db;

        protected override async Task<Result> HandleCore(NewTickerCommand request, CancellationToken cancellationToken)
        {
            var bots = await _db.Bots
                .Where(b => b.Enabled)
                .ToListAsync(cancellationToken);

            // Process the bots as needed

            return Result.Success;
        }
    }
}