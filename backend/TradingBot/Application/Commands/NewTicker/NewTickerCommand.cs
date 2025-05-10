using MediatR;
using TradingBot.Application.Common;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application;

public class NewTickerCommand : IRequest<Result>
{
    public required Ticker Ticker { get; set; }

    public class NewTickerCommandHandler(
        IBackgroundJobProcessor backgroundJobProcessor,
        ILogger<NewTickerCommandHandler> logger) : BaseCommandHandler<NewTickerCommand>(logger)
    {
        private readonly IBackgroundJobProcessor _backgroundJobProcessor = backgroundJobProcessor;
        private readonly ILogger<NewTickerCommandHandler> _logger = logger;

        protected override Task<Result> HandleCore(NewTickerCommand request, CancellationToken cancellationToken)
        {
            var ticker = request.Ticker;
            _logger.LogDebug("Processing ticker update for {Symbol}", ticker.Symbol);
        
            _backgroundJobProcessor.Enqueue(new PlaceEntryOrdersCommand { Ticker = ticker });
            _backgroundJobProcessor.Enqueue(new PlaceExitOrdersCommand { Ticker = ticker });

            return Task.FromResult(Result.Success);
        }
    }
}