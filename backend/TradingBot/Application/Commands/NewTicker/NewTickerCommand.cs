using MediatR;
using TradingBot.Application.Common;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Application.Commands.SaveTicker;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application;

public class NewTickerCommand : IRequest<Result>
{
    public required TickerDto TickerDto { get; set; }

    public class NewTickerCommandHandler(
        IBackgroundJobProcessor backgroundJobProcessor,
        ILogger<NewTickerCommandHandler> logger) : BaseCommandHandler<NewTickerCommand>(logger)
    {
        private readonly IBackgroundJobProcessor _backgroundJobProcessor = backgroundJobProcessor;
        private readonly ILogger<NewTickerCommandHandler> _logger = logger;

        protected override Task<Result> HandleCore(NewTickerCommand request, CancellationToken cancellationToken)
        {
            var tickerDto = request.TickerDto;
            _logger.LogDebug("Processing ticker update for {Symbol}", tickerDto.Symbol);
        
            // Save ticker data for historical records
            _backgroundJobProcessor.Enqueue(new SaveTickerCommand { TickerDto = tickerDto });
            
            // Process trading logic
            _backgroundJobProcessor.Enqueue(new PlaceEntryOrdersCommand { Ticker = tickerDto });
            _backgroundJobProcessor.Enqueue(new PlaceExitOrdersCommand { Ticker = tickerDto });

            return Task.FromResult(Result.Success);
        }
    }
}