using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Services;
using TradingBot.Application.Common;
using System.Collections.Concurrent;

namespace TradingBot.Application.Commands.VerifyBotBalance;

public class VerifyBalancesCommand : IRequest<Result>
{
    public class VerifyBalancesCommandHandler : BaseCommandHandler<VerifyBalancesCommand>
    {
        private readonly IDbContextFactory<TradingBotDbContext> _dbContextFactory;
        private readonly IExchangeApiRepository _exchangeApiRepository;
        private readonly ILogger<VerifyBalancesCommandHandler> _logger;
        private readonly int _maxRetries = 5;
        private readonly TimeSpan _delayBetweenChecks = TimeSpan.FromSeconds(5);

        public VerifyBalancesCommandHandler(
            IDbContextFactory<TradingBotDbContext> dbContextFactory,
            IExchangeApiRepository exchangeApiRepository,
            ILogger<VerifyBalancesCommandHandler> logger)
            : base(logger)
        {
            _dbContextFactory = dbContextFactory;
            _exchangeApiRepository = exchangeApiRepository;
            _logger = logger;
        }

        // Convenience constructor used by existing unit tests
        internal VerifyBalancesCommandHandler(
            TradingBotDbContext dbContext,
            IExchangeApiRepository exchangeApiRepository,
            ILogger<VerifyBalancesCommandHandler> logger)
            : this(new SingleDbContextFactory(dbContext), exchangeApiRepository, logger)
        {
        }

        private sealed class SingleDbContextFactory(TradingBotDbContext ctx) : IDbContextFactory<TradingBotDbContext>
        {
            public TradingBotDbContext CreateDbContext() => ctx;
        }

        protected override async Task<Result> HandleCore(VerifyBalancesCommand request, CancellationToken cancellationToken)
        {
            // Fetch enabled bot IDs at the start
            using var dbContext = _dbContextFactory.CreateDbContext();
            var enabledBotIds = await dbContext.Bots
                .Where(b => b.Enabled)
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Verifying balances for {BotCount} enabled bots", enabledBotIds.Count);

            var errorBag = new ConcurrentBag<string>();

            var tasks = enabledBotIds.Select(botId => Task.Run(async () =>
            {
                try
                {
                    int attemptCount = 0;
                    while (attemptCount < _maxRetries)
                    {
                        attemptCount++;
                        using var context = _dbContextFactory.CreateDbContext();
                        var botWithBalance = await context.Bots
                            .Where(b => b.Id == botId && b.Enabled)
                            .Select(b => new
                            {
                                Bot = b,
                                ExpectedBalance = b.StartingBaseAmount
                                    + b.Trades
                                        .Where(t => t.ExitOrder == null || t.ExitOrder.Status != OrderStatus.Filled)
                                        .Sum(t => t.EntryOrder.QuantityFilled * (t.EntryOrder.IsBuy ? 1m : -1m))
                                    + b.Trades
                                        .Where(t => t.ExitOrder != null && t.ExitOrder.Status != OrderStatus.Filled)
                                        .Sum(t => t.ExitOrder!.QuantityFilled * (t.ExitOrder!.IsBuy ? 1m : -1m))
                            })
                            .FirstOrDefaultAsync(cancellationToken);

                        if (botWithBalance == null)
                        {
                            _logger.LogWarning("Bot {BotId} is no longer enabled or does not exist.", botId);
                            break;
                        }

                        var bot = botWithBalance.Bot;
                        var expectedBalance = botWithBalance.ExpectedBalance;
                        var exchangeApi = _exchangeApiRepository.GetExchangeApi(bot);
                        var symbol = bot.Symbol;
                        var baseCurrency = CurrencyUtilities.ExtractBaseCurrency(symbol);

                        _logger.LogInformation("Attempt {Attempt}/{MaxRetries} to verify balance for bot {BotId}", attemptCount, _maxRetries, bot.Id);

                        try
                        {
                            var actualBalance = await exchangeApi.GetBalance(baseCurrency, bot, cancellationToken);

                            if (actualBalance == expectedBalance)
                            {
                                _logger.LogInformation("Balance verification succeeded for bot {BotId}. Balance: {Balance}", bot.Id, actualBalance);
                                break;
                            }

                            if (attemptCount < _maxRetries)
                            {
                                _logger.LogWarning("Balance mismatch for bot {BotId}. Expected: {Expected}, Actual: {Actual}. Will retry.",
                                    bot.Id,
                                    expectedBalance,
                                    actualBalance);

                                await Task.Delay(_delayBetweenChecks, cancellationToken);
                            }
                            else
                            {
                                _logger.LogCritical("Balance verification failed for bot {BotId} after {MaxRetries} attempts. Bot disabled.", bot.Id, _maxRetries);
                                bot.Enabled = false;
                                await context.SaveChangesAsync(cancellationToken);
                                errorBag.Add($"Balance verification failed for bot {bot.Id}: Expected {expectedBalance}, Actual {actualBalance}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error verifying balance for bot {BotId} on attempt {Attempt}", bot.Id, attemptCount);

                            if (attemptCount >= _maxRetries)
                            {
                                _logger.LogCritical("Balance verification failed for bot {BotId} due to exceptions. Bot disabled.", bot.Id);
                                bot.Enabled = false;
                                await context.SaveChangesAsync(cancellationToken);
                                errorBag.Add($"Balance verification failed for bot {bot.Id}: {ex.Message}");
                            }

                            await Task.Delay(_delayBetweenChecks, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in parallel bot verification for bot {BotId}", botId);
                    errorBag.Add($"Unexpected error for bot {botId}: {ex.Message}");
                }
            }, cancellationToken)).ToList();

            await Task.WhenAll(tasks);

            return errorBag.IsEmpty ? Result.Success : Result.Failure([.. errorBag]);
        }
    }
}