using MediatR;
using TradingBot.Data;

namespace TradingBot.Application;

public class PlaceOpeningOrdersCommand : IRequest
{
    public required Ticker Ticker { get; set; }

    public class PlaceOpeningOrdersCommandHandler : IRequestHandler<PlaceOpeningOrdersCommand>
    {
        public Task Handle(PlaceOpeningOrdersCommand request, CancellationToken cancellationToken)
        {
            var
        }
    }