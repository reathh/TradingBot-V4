using MediatR;
using TradingBot.Application.Common;
using TradingBot.Data;

namespace TradingBot.Application.Commands.SaveTicker;

public class SaveTickerCommand : IRequest<Result>
{
    public required Ticker Ticker { get; set; }

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
                var ticker = request.Ticker;
                _logger.LogDebug("Saving ticker data for {Symbol}: Bid={Bid}, Ask={Ask}, Last={Last}", 
                    ticker.Symbol, ticker.Bid, ticker.Ask, ticker.LastPrice);

                var tickerEntity = new TickerEntity(ticker);
                
                await _dbContext.Tickers.AddAsync(tickerEntity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogDebug("Successfully saved ticker data for {Symbol} at {Timestamp}", ticker.Symbol, ticker.Timestamp);
                
                return Result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving ticker data");
                return "Failed to save ticker data: " + ex.Message;
            }
        }
    }
} 