using MediatR;
using TradingBot.Application.Common;
using TradingBot.Data;

namespace TradingBot.Application.Commands.SaveTicker;

public class SaveTickerCommand : IRequest<Result>
{
    public required TickerDto TickerDto { get; init; }

    public class SaveTickerCommandHandler(
        TradingBotDbContext dbContext,
        ILogger<SaveTickerCommandHandler> logger) : BaseCommandHandler<SaveTickerCommand>(logger)
    {
        private readonly TradingBotDbContext _dbContext = dbContext;
        private readonly ILogger<SaveTickerCommandHandler> _logger = logger;

        protected override async Task<Result> HandleCore(SaveTickerCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var tickerDto = request.TickerDto;
                _logger.LogDebug("Saving ticker data for {Symbol}: Bid={Bid}, Ask={Ask}, Last={Last}", 
                    tickerDto.Symbol, tickerDto.Bid, tickerDto.Ask, tickerDto.LastPrice);

                var ticker = new Ticker(tickerDto);
                
                await _dbContext.Tickers.AddAsync(ticker, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogDebug("Successfully saved ticker data for {Symbol} at {Timestamp}", tickerDto.Symbol, tickerDto.Timestamp);
                
                return Result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving ticker data");
                return Result.Failure(["Failed to save ticker data: " + ex.Message]);
            }
        }
    }
} 