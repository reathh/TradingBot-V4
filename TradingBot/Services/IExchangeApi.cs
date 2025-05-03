using TradingBot.Data;

namespace TradingBot.Services;

public interface IExchangeApi
{
    Task<Order> PlaceOrder(Bot bot, decimal price, decimal quantity, bool isBuy, CancellationToken cancellationToken);
} 