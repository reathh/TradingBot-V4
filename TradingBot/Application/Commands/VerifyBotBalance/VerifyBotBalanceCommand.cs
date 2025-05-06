using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Services;
using TradingBot.Application.Common;
using Microsoft.Extensions.Logging;

namespace TradingBot.Application.Commands.VerifyBotBalance;

public class VerifyBotBalanceCommand : IRequest<Result>
{
    public required Bot Bot { get; set; }
    public required decimal CurrentPrice { get; set; }

    public class VerifyBotBalanceCommandHandler : BaseCommandHandler<VerifyBotBalanceCommand>
    {
        private readonly TradingBotDbContext _dbContext;
        private readonly IExchangeApiRepository _exchangeApiRepository;
        private readonly int _maxRetries = 5;
        private readonly TimeSpan _delayBetweenChecks = TimeSpan.FromSeconds(5);
        private readonly ILogger<VerifyBotBalanceCommandHandler> _logger;

        public VerifyBotBalanceCommandHandler(
            TradingBotDbContext dbContext,
            IExchangeApiRepository exchangeApiRepository,
            ILogger<VerifyBotBalanceCommandHandler> logger) : base(logger)
        {
            _dbContext = dbContext;
            _exchangeApiRepository = exchangeApiRepository;
            _logger = logger;
        }

        protected override async Task<Result> HandleCore(VerifyBotBalanceCommand request, CancellationToken cancellationToken)
        {
            var bot = request.Bot;
            var exchangeApi = _exchangeApiRepository.GetExchangeApi(bot);
            var symbol = bot.Symbol;

            // Extract base currency from symbol (e.g., BTC from BTCUSDT)
            var baseCurrency = CurrencyUtilities.ExtractBaseCurrency(symbol);

            decimal expectedBalance;
            decimal actualBalance;
            bool balanceVerified = false;
            var attempts = 0;

            // Try multiple times to verify balance
            while (!balanceVerified && attempts < _maxRetries)
            {
                try
                {
                    // Calculate expected balance
                    expectedBalance = CalculateExpectedBalance(bot, request.CurrentPrice);

                    // Get actual balance (simple call, no retry mechanism)
                    actualBalance = await GetActualBalance(exchangeApi, baseCurrency, bot, cancellationToken);

                    // Check if balances match
                    if (actualBalance == expectedBalance)
                    {
                        balanceVerified = true;
                        break;
                    }

                    // If balances don't match and we haven't reached max attempts,
                    // wait and then try again (balances might change during execution)
                    if (attempts < _maxRetries - 1)
                    {
                        _logger.LogInformation("Balance mismatch for bot {BotId}. Expected: {Expected}, Actual: {Actual}. Retrying...",
                            bot.Id, expectedBalance, actualBalance);
                        await Task.Delay(_delayBetweenChecks, cancellationToken);
                    }
                    else
                    {
                        // On last attempt, disable bot if balances still don't match
                        bot.Enabled = false;
                        _logger.LogCritical("Balance verification failed for bot {BotId} after {Attempts} attempts. Bot disabled.",
                            bot.Id, attempts + 1);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        return $"Balance verification failed for bot {bot.Id}: Expected {expectedBalance}, Actual {actualBalance}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying balance for bot {BotId} on attempt {Attempt}", bot.Id, attempts + 1);

                    if (attempts >= _maxRetries - 1)
                    {
                        bot.Enabled = false;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        return $"Balance verification failed for bot {bot.Id}: {ex.Message}";
                    }

                    await Task.Delay(_delayBetweenChecks, cancellationToken);
                }

                attempts++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }

        private static decimal CalculateExpectedBalance(Bot bot, decimal currentPrice)
        {
            decimal expectedBalance = bot.StartingBaseAmount;
            decimal? referencePrice = null;

            // Find the first open trade (where profit is null) to use as reference price
            var firstOpenTrade = bot.Trades.FirstOrDefault(t => t.Profit == null);
            if (firstOpenTrade != null)
            {
                referencePrice = firstOpenTrade.EntryOrder.Price;
            }
            else
            {
                // If there are no open trades, return the starting base amount
                return bot.StartingBaseAmount;
            }

            // Calculate expected balance based on price difference
            decimal priceDifference = Math.Abs(referencePrice.Value - currentPrice);
            decimal stepCount = Math.Floor(priceDifference / bot.EntryStep);

            if (bot.IsLong)
            {
                // For long bot: if current price is lower than reference price, we should have bought
                if (currentPrice < referencePrice.Value)
                {
                    expectedBalance += stepCount * bot.EntryQuantity;
                }
            }
            else
            {
                // For short bot: if current price is higher than reference price, we should have sold
                if (currentPrice > referencePrice.Value)
                {
                    expectedBalance -= stepCount * bot.EntryQuantity;
                }
            }

            // Calculate actual trade adjustments based on filled orders
            decimal tradeAdjustment = 0;

            // Adjust for the entry orders
            foreach (var trade in bot.Trades)
            {
                // Add buy orders to expected balance
                if (trade.EntryOrder.IsBuy)
                {
                    tradeAdjustment += trade.EntryOrder.QuantityFilled;
                }
                // Subtract sell orders from expected balance
                else
                {
                    tradeAdjustment -= trade.EntryOrder.QuantityFilled;
                }
            }

            // Adjust for the exit orders
            foreach (var trade in bot.Trades.Where(t => t.ExitOrder != null))
            {
                // Subtract sell exit orders from expected balance
                if (trade.ExitOrder!.IsBuy == false)
                {
                    tradeAdjustment -= trade.ExitOrder.QuantityFilled;
                }
                // Add buy exit orders to expected balance
                else
                {
                    tradeAdjustment += trade.ExitOrder.QuantityFilled;
                }
            }

            // Apply the trade adjustment
            expectedBalance += tradeAdjustment;

            return expectedBalance;
        }

        private async Task<decimal> GetActualBalance(
            IExchangeApi exchangeApi,
            string baseCurrency,
            Bot bot,
            CancellationToken cancellationToken) => await exchangeApi.GetBalance(baseCurrency, bot, cancellationToken);
    }
}