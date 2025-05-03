using TradingBot.Data;
using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;

namespace TradingBot.Services;

public class BinanceExchangeApi(TimeProvider timeProvider) : IExchangeApi
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Order> PlaceOrder(Bot bot, decimal price, decimal quantity, bool isBuy, CancellationToken cancellationToken)
    {
        using var client = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(bot.PublicKey, bot.PrivateKey);
        });

        var order = await client.SpotApi.Trading.PlaceOrderAsync(
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
            symbol: bot.Symbol,
            price: order.Data.Price,
            quantity: order.Data.Quantity,
            isBuy: order.Data.Side == OrderSide.Buy,
            createdAt: _timeProvider.GetUtcNow().DateTime)
        {
            ExchangeOrderId = order.Data.Id.ToString()
        };
    }
} 