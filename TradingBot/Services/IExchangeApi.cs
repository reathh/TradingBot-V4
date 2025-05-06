using TradingBot.Data;

namespace TradingBot.Services;

public interface IExchangeApi
{
    Task<Order> PlaceOrder(Bot bot, decimal price, decimal quantity, bool isBuy, CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to real-time order updates from the exchange
    /// </summary>
    /// <param name="callback">Callback to be invoked when an order is updated</param>
    /// <param name="bot">The bot for which to subscribe to order updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SubscribeToOrderUpdates(Func<OrderUpdate, Task> callback, Bot bot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of an order from the exchange
    /// </summary>
    /// <param name="orderId">The order ID (either internal or exchange ID)</param>
    /// <param name="bot">The bot that placed the order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated order information</returns>
    Task<OrderUpdate> GetOrderStatus(string orderId, Bot bot, CancellationToken cancellationToken = default);
}