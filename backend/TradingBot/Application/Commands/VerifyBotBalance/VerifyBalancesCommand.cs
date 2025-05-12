using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Services;
using TradingBot.Application.Common;

namespace TradingBot.Application.Commands.VerifyBotBalance;

public class VerifyBalancesCommand : IRequest<Result>
{
    public class VerifyBalancesCommandHandler(
        TradingBotDbContext dbContext,
        IExchangeApiRepository exchangeApiRepository,
        ILogger<VerifyBalancesCommandHandler> logger) : BaseCommandHandler<VerifyBalancesCommand>(logger)
    {
        private readonly int _maxRetries = 5;
        private readonly TimeSpan _delayBetweenChecks = TimeSpan.FromSeconds(5);

        protected override async Task<Result> HandleCore(VerifyBalancesCommand request, CancellationToken cancellationToken)
        {
            var bots = await dbContext
                .Bots
                .Where(b => b.Enabled)
                .Include(b => b.Trades)
                .ThenInclude(t => t.EntryOrder)
                .Include(b => b.Trades)
                .ThenInclude(t => t.ExitOrder)
                .ToListAsync(cancellationToken);

            logger.LogDebug("Verifying balances for {BotCount} enabled bots", bots.Count);

            var errors = new List<string>();

            foreach (var bot in bots)
            {
                var exchangeApi = exchangeApiRepository.GetExchangeApi(bot);
                var symbol = bot.Symbol;
                var baseCurrency = CurrencyUtilities.ExtractBaseCurrency(symbol);
                decimal expectedBalance = CalculateExpectedBalance(bot);
                int attemptCount = 0;

                while (attemptCount < _maxRetries)
                {
                    attemptCount++;
                    logger.LogInformation("Attempt {Attempt}/{MaxRetries} to verify balance for bot {BotId}", attemptCount, _maxRetries, bot.Id);

                    try
                    {
                        var actualBalance = await exchangeApi.GetBalance(baseCurrency, bot, cancellationToken);

                        if (actualBalance == expectedBalance)
                        {
                            logger.LogInformation("Balance verification succeeded for bot {BotId}. Balance: {Balance}", bot.Id, actualBalance);

                            break;
                        }

                        if (attemptCount < _maxRetries)
                        {
                            logger.LogWarning("Balance mismatch for bot {BotId}. Expected: {Expected}, Actual: {Actual}. Will retry.",
                                bot.Id,
                                expectedBalance,
                                actualBalance);

                            await Task.Delay(_delayBetweenChecks, cancellationToken);
                        }
                        else
                        {
                            logger.LogCritical("Balance verification failed for bot {BotId} after {MaxRetries} attempts. Bot disabled.", bot.Id, _maxRetries);
                            bot.Enabled = false;
                            await dbContext.SaveChangesAsync(cancellationToken);
                            errors.Add($"Balance verification failed for bot {bot.Id}: Expected {expectedBalance}, Actual {actualBalance}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error verifying balance for bot {BotId} on attempt {Attempt}", bot.Id, attemptCount);

                        if (attemptCount >= _maxRetries)
                        {
                            logger.LogCritical("Balance verification failed for bot {BotId} due to exceptions. Bot disabled.", bot.Id);
                            bot.Enabled = false;
                            await dbContext.SaveChangesAsync(cancellationToken);
                            errors.Add($"Balance verification failed for bot {bot.Id}: {ex.Message}");
                        }

                        await Task.Delay(_delayBetweenChecks, cancellationToken);
                    }
                }
            }

            return errors.Count == 0 ? Result.Success : Result.Failure(errors);
        }

        private static decimal CalculateExpectedBalance(Bot bot)
        {
            decimal expectedBalance = bot.StartingBaseAmount;

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
    }
}