using TradingBot.Data;

namespace TradingBot.Services;

/// <summary>
/// Repository for managing and reusing BinanceExchangeApi instances
/// </summary>
public class BinanceExchangeApiRepository : IExchangeApiRepository
{
    private readonly Dictionary<string, IExchangeApi> _apiInstances = new();
    private readonly TimeProvider _timeProvider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly object _lock = new();

    public BinanceExchangeApiRepository(
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory)
    {
        _timeProvider = timeProvider;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Gets or creates an IExchangeApi instance for the specified bot
    /// </summary>
    /// <param name="bot">Bot containing API credentials</param>
    /// <returns>A reusable exchange API instance</returns>
    public IExchangeApi GetExchangeApi(Bot bot)
    {
        // Use the public/private key pair as a unique identifier
        string key = $"{bot.PublicKey}:{bot.PrivateKey}";

        lock (_lock)
        {
            if (!_apiInstances.TryGetValue(key, out var api))
            {
                // Create new instance if it doesn't exist
                var logger = _loggerFactory.CreateLogger<BinanceExchangeApi>();
                api = new BinanceExchangeApi(
                    bot.PublicKey,
                    bot.PrivateKey,
                    _timeProvider,
                    logger);

                _apiInstances[key] = api;
            }

            return api;
        }
    }
}