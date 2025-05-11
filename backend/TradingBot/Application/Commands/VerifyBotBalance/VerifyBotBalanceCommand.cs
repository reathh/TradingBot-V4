using MediatR;
using TradingBot.Data;
using TradingBot.Services;
using TradingBot.Application.Common;

namespace TradingBot.Application.Commands.VerifyBotBalance;

public class VerifyBotBalanceCommand : IRequest<Result>
{
    public required Bot Bot { get; set; }
    public required decimal CurrentPrice { get; set; }

    public class VerifyBotBalanceCommandHandler(
        TradingBotDbContext dbContext,
        IExchangeApiRepository exchangeApiRepository,
        ILogger<VerifyBotBalanceCommandHandler> logger) : BaseCommandHandler<VerifyBotBalanceCommand>(logger)
    {
        private readonly TradingBotDbContext _dbContext = dbContext;
        private readonly IExchangeApiRepository _exchangeApiRepository = exchangeApiRepository;
        private readonly int _maxRetries = 5;
        private readonly TimeSpan _delayBetweenChecks = TimeSpan.FromSeconds(5);
        private readonly ILogger<VerifyBotBalanceCommandHandler> _logger = logger;

        protected override async Task<Result> HandleCore(VerifyBotBalanceCommand request, CancellationToken cancellationToken)
        {
            var bot = request.Bot;
            var exchangeApi = _exchangeApiRepository.GetExchangeApi(bot);
            var symbol = bot.Symbol;

            // Extract base currency from symbol (e.g., BTC from BTCUSDT)
            var baseCurrency = CurrencyUtilities.ExtractBaseCurrency(symbol);

            // Calculate expected balance once - this doesn't change during retries
            decimal expectedBalance = CalculateExpectedBalance(bot, request.CurrentPrice);
            decimal actualBalance = 0;
            int attemptCount = 0;

            // Make retries explicit - try up to _maxRetries times to get matching balances
            while (attemptCount < _maxRetries)
            {
                attemptCount++;
                _logger.LogInformation("Attempt {Attempt}/{MaxRetries} to verify balance for bot {BotId}", 
                    attemptCount, _maxRetries, bot.Id);

                try
                {
                    // Get actual balance from exchange
                    actualBalance = await exchangeApi.GetBalance(baseCurrency, bot, cancellationToken);
                    
                    // Check if balance verification succeeded
                    if (actualBalance == expectedBalance)
                    {
                        _logger.LogInformation("Balance verification succeeded for bot {BotId}. Balance: {Balance}", 
                            bot.Id, actualBalance);
                        return Result.Success;
                    }
                    
                    // If not the last retry, log and continue
                    if (attemptCount < _maxRetries)
                    {
                        _logger.LogWarning("Balance mismatch for bot {BotId}. Expected: {Expected}, Actual: {Actual}. Will retry.",
                            bot.Id, expectedBalance, actualBalance);
                        
                        await Task.Delay(_delayBetweenChecks, cancellationToken);
                    }
                    else
                    {
                        // This was the last attempt and it failed
                        _logger.LogCritical("Balance verification failed for bot {BotId} after {MaxRetries} attempts. Bot disabled.",
                            bot.Id, _maxRetries);
                        
                        // Disable the bot since balance verification failed
                        bot.Enabled = false;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        
                        return $"Balance verification failed for bot {bot.Id}: Expected {expectedBalance}, Actual {actualBalance}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying balance for bot {BotId} on attempt {Attempt}", bot.Id, attemptCount);
                    
                    if (attemptCount >= _maxRetries)
                    {
                        _logger.LogCritical("Balance verification failed for bot {BotId} due to exceptions. Bot disabled.", bot.Id);
                        bot.Enabled = false;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        return $"Balance verification failed for bot {bot.Id}: {ex.Message}";
                    }
                    
                    await Task.Delay(_delayBetweenChecks, cancellationToken);
                }
            }

            // Should never reach here, but just in case
            return Result.Success;
        }

        private static decimal CalculateExpectedBalance(Bot bot, decimal currentPrice)
        {
            // Start with the initial balance
            decimal expectedBalance = bot.StartingBaseAmount;

            // Calculate adjustments based on actual filled orders
            // This approach is more reliable than theoretical calculations
            
            // Adjust for all entry orders (buy orders add to balance, sell orders subtract)
            foreach (var trade in bot.Trades)
            {
                if (trade.EntryOrder.IsBuy)
                {
                    expectedBalance += trade.EntryOrder.QuantityFilled;
                }
                else
                {
                    expectedBalance -= trade.EntryOrder.QuantityFilled;
                }
            }

            // Adjust for all exit orders (buy orders add to balance, sell orders subtract)
            foreach (var trade in bot.Trades.Where(t => t.ExitOrder != null))
            {
                if (trade.ExitOrder!.IsBuy)
                {
                    expectedBalance += trade.ExitOrder.QuantityFilled;
                }
                else
                {
                    expectedBalance -= trade.ExitOrder.QuantityFilled;
                }
            }

            return expectedBalance;
        }

        private async Task<decimal> GetActualBalance(
            IExchangeApi exchangeApi,
            string baseCurrency,
            Bot bot,
            CancellationToken cancellationToken) => await exchangeApi.GetBalance(baseCurrency, bot, cancellationToken);
    }
}