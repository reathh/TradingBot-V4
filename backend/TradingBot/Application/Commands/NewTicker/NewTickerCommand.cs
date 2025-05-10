using MediatR;
using TradingBot.Data;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Common;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Application.Commands.PlaceExitOrders;

namespace TradingBot.Application;

public class NewTickerCommand : IRequest<Result>
{
    public required Ticker Ticker { get; set; }

    public class NewTickerCommandHandler : BaseCommandHandler<NewTickerCommand>
    {
        private readonly TradingBotDbContext _db;
        private readonly IMediator _mediator;
        private readonly ILogger<NewTickerCommandHandler> _logger;

        public NewTickerCommandHandler(
            TradingBotDbContext db,
            IMediator mediator,
            ILogger<NewTickerCommandHandler> logger) : base(logger)
        {
            _db = db;
            _mediator = mediator;
            _logger = logger;
        }

        protected override async Task<Result> HandleCore(NewTickerCommand request, CancellationToken cancellationToken)
        {
            var ticker = request.Ticker;
            _logger.LogDebug("Processing ticker update for {Symbol}: Bid={Bid}, Ask={Ask}, Last={Last}",
                ticker.Symbol, ticker.Bid, ticker.Ask, ticker.LastPrice);

            // Get bots for this symbol
            var bots = await _db.Bots
                .Where(b => b.Enabled && b.Symbol == ticker.Symbol)
                .ToListAsync(cancellationToken);

            if (bots.Count == 0)
            {
                _logger.LogDebug("No enabled bots found for symbol {Symbol}", ticker.Symbol);
                return Result.Success;
            }

            _logger.LogInformation("Processing ticker update for {Count} bots with symbol {Symbol}",
                bots.Count, ticker.Symbol);

            // Process entry and exit orders
            await ProcessEntryOrders(ticker, cancellationToken);
            await ProcessExitOrders(ticker, cancellationToken);

            return Result.Success;
        }

        private async Task ProcessEntryOrders(Ticker ticker, CancellationToken cancellationToken)
        {
            try
            {
                var command = new PlaceEntryOrdersCommand { Ticker = ticker };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to place entry orders for {Symbol}: {Errors}",
                        ticker.Symbol, string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing entry orders for {Symbol}", ticker.Symbol);
            }
        }

        private async Task ProcessExitOrders(Ticker ticker, CancellationToken cancellationToken)
        {
            try
            {
                var command = new PlaceExitOrdersCommand { Ticker = ticker };
                var result = await _mediator.Send(command, cancellationToken);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to place exit orders for {Symbol}: {Errors}",
                        ticker.Symbol, string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing exit orders for {Symbol}", ticker.Symbol);
            }
        }
    }
}