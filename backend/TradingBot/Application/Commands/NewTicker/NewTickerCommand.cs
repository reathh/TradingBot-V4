namespace TradingBot.Application.Commands.NewTicker;

using MediatR;
using Models;
using PlaceEntryOrders;
using PlaceExitOrders;
using SaveTicker;
using Common;
using Services;

public class NewTickerCommand : IRequest<Result>
{
    public required TickerDto Ticker { get; set; }

    public class NewTickerCommandHandler(IBackgroundJobProcessor backgroundJobProcessor, ILogger<NewTickerCommandHandler> logger)
        : BaseCommandHandler<NewTickerCommand>(logger)
    {
        protected override Task<Result> HandleCore(NewTickerCommand request, CancellationToken cancellationToken)
        {
            var tickerDto = request.Ticker;
            logger.LogDebug("Processing ticker update for {Symbol}", tickerDto.Symbol);

            // Save ticker data for historical records
            backgroundJobProcessor.Enqueue(new SaveTickerCommand
            {
                TickerDto = tickerDto
            });

            // Process trading logic
            backgroundJobProcessor.Enqueue(new PlaceEntryOrdersCommand
            {
                Ticker = tickerDto
            });

            backgroundJobProcessor.Enqueue(new PlaceExitOrdersCommand
            {
                Ticker = tickerDto
            });

            return Task.FromResult(Result.Success);
        }
    }
}