using TradingBot.Data;

namespace TradingBot.Services;

/// <summary>
/// Interface for a repository that manages exchange API instances
/// </summary>
public interface IExchangeApiRepository
{
    /// <summary>
    /// Gets an IExchangeApi instance for the specified bot
    /// </summary>
    /// <param name="bot">Bot containing API credentials</param>
    /// <returns>An exchange API instance configured for the bot</returns>
    IExchangeApi GetExchangeApi(Bot bot);
}