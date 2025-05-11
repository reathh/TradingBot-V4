using TradingBot.Data;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Authentication;

namespace TradingBot.Services;

public class BinanceExchangeApi(
    string publicKey,
    string privateKey,
    TimeProvider timeProvider,
    ILogger<BinanceExchangeApi> logger) : IExchangeApi
{
    private readonly Dictionary<string, List<Func<OrderUpdate, Task>>> _orderCallbacks = [];
    private readonly Dictionary<string, bool> _userDataSubscribed = [];
    private readonly BinanceRestClient _restClient = new(options =>
    {
        options.ApiCredentials = new ApiCredentials(publicKey, privateKey);
    });
    private readonly BinanceSocketClient _socketClient = new(options =>
    {
        options.ApiCredentials = new ApiCredentials(publicKey, privateKey);
    });

    public async Task<Order> PlaceOrder(Bot bot, decimal price, decimal quantity, bool isBuy, CancellationToken cancellationToken)
    {
        var order = await _restClient.SpotApi.Trading.PlaceOrderAsync(
            symbol: bot.Symbol,
            side: isBuy ? OrderSide.Buy : OrderSide.Sell,
            type: SpotOrderType.Limit,
            quantity: quantity,
            price: price,
            timeInForce: TimeInForce.GoodTillCanceled,
            ct: cancellationToken);

        if (!order.Success)
        {
            throw new Exception($"Failed to place order: {order.Error?.Message}");
        }

        return new Order(
            order.Data.Id.ToString(),
            bot.Symbol,
            order.Data.Price,
            order.Data.Quantity,
            order.Data.Side == OrderSide.Buy,
            timeProvider.GetUtcNow().DateTime);
    }

    public async Task SubscribeToOrderUpdates(Func<OrderUpdate, Task> callback, Bot bot, CancellationToken cancellationToken = default)
    {
        // Add callback to the list
        string key = $"{bot.PublicKey}:{bot.PrivateKey}";
        if (!_orderCallbacks.TryGetValue(key, out List<Func<OrderUpdate, Task>>? value))
        {
            value = [];
            _orderCallbacks[key] = value;
        }

        value.Add(callback);

        // If already subscribed for this bot, we're done
        if (_userDataSubscribed.TryGetValue(key, out bool existingValue) && existingValue)
        {
            return;
        }

        // Get listen key for user data stream
        var response = await _restClient.SpotApi.Account.StartUserStreamAsync(cancellationToken);
        if (!response.Success)
        {
            throw new Exception($"Could not start user data stream: {response.Error?.Message}");
        }

        var listenKey = response.Data;
        logger.LogInformation("Started user data stream with listen key {ListenKey}", listenKey);

        // Subscribe to user data updates
        var subscriptionResult = await _socketClient.SpotApi.Account.SubscribeToUserDataUpdatesAsync(
            listenKey,
            data =>
            {
                if (data.Data is BinanceStreamOrderUpdate orderUpdate)
                {
                    var update = MapBinanceOrderToOrderUpdate(orderUpdate);
                    logger.LogTrace("Order update received: {@OrderUpdate}", update);

                    if (_orderCallbacks.ContainsKey(key))
                    {
                        foreach (var cb in _orderCallbacks[key])
                        {
                            _ = cb(update);
                        }
                    }
                }
            },
            ct: cancellationToken);

        if (!subscriptionResult.Success)
        {
            throw new Exception($"Could not subscribe to user data updates: {subscriptionResult.Error?.Message}");
        }

        // Start keep-alive task to maintain the listen key
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
                    await _restClient.SpotApi.Account.KeepAliveUserStreamAsync(listenKey, cancellationToken);
                    logger.LogDebug("Refreshed listen key {ListenKey}", listenKey);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error refreshing listen key {ListenKey}", listenKey);
                }
            }
        }, cancellationToken);

        _userDataSubscribed[key] = true;
    }

    private static OrderUpdate MapBinanceOrderToOrderUpdate(BinanceStreamOrderUpdate order)
    {
        return new OrderUpdate(
            Id: order.Id.ToString(),
            Symbol: order.Symbol,
            Price: order.Price,
            Quantity: order.Quantity,
            QuantityFilled: order.QuantityFilled,
            AverageFillPrice: order.QuoteQuantityFilled == 0 ? null : order.QuoteQuantityFilled / order.QuantityFilled,
            IsBuy: order.Side == OrderSide.Buy,
            Canceled: order.Status == OrderStatus.Canceled,
            Closed: IsOrderClosed(order.Status)
        );
    }

    public async Task<OrderUpdate> GetOrderStatus(string orderId, Bot bot, CancellationToken cancellationToken = default)
    {
        // Try to parse the order ID as a long (Binance order IDs are numeric)
        if (!long.TryParse(orderId, out var binanceOrderId))
        {
            throw new ArgumentException($"Invalid order ID format: {orderId}. Expected a numeric value.", nameof(orderId));
        }

        // Query the order status from Binance
        var orderResult = await _restClient.SpotApi.Trading.GetOrderAsync(
            symbol: bot.Symbol,
            orderId: binanceOrderId,
            ct: cancellationToken);

        if (!orderResult.Success)
        {
            throw new Exception($"Failed to get order status: {orderResult.Error?.Message}");
        }

        var order = orderResult.Data;

        // Map the Binance order to our OrderUpdate model
        return new OrderUpdate(
            Id: order.Id.ToString(),
            Symbol: order.Symbol,
            Price: order.Price,
            Quantity: order.Quantity,
            QuantityFilled: order.QuantityFilled,
            AverageFillPrice: order.AverageFillPrice > 0 ? order.AverageFillPrice : null,
            IsBuy: order.Side == OrderSide.Buy,
            Canceled: order.Status == OrderStatus.Canceled,
            Closed: IsOrderClosedStatus(order.Status)
        );
    }

    public async Task<decimal> GetBalance(string asset, Bot bot, CancellationToken cancellationToken = default)
    {
        var balanceResult = await _restClient.SpotApi.Account.GetAccountInfoAsync(ct: cancellationToken);

        if (!balanceResult.Success)
        {
            throw new Exception($"Failed to get balance information: {balanceResult.Error?.Message}");
        }

        var assetBalance = balanceResult.Data.Balances.FirstOrDefault(b => b.Asset == asset);

        if (assetBalance == null)
        {
            logger.LogWarning("Asset {Asset} not found in account balances", asset);
            return 0;
        }

        return assetBalance.Total;
    }

    private static bool IsOrderClosed(OrderStatus status) => IsOrderClosedStatus(status);

    private static bool IsOrderClosedStatus(OrderStatus status) =>
        status is OrderStatus.Filled or OrderStatus.Canceled or OrderStatus.Expired or OrderStatus.Rejected;
}