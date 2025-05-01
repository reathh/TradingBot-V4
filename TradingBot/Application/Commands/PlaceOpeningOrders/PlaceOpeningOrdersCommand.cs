using MediatR;
using TradingBot.Data;
using Microsoft.EntityFrameworkCore;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;

namespace TradingBot.Application;

public class PlaceOpeningOrdersCommand : IRequest
{
    public required Ticker Ticker { get; set; }

    public class PlaceOpeningOrdersCommandHandler(TradingBotDbContext dbContext) : IRequestHandler<PlaceOpeningOrdersCommand>
    {
        private readonly TradingBotDbContext db = dbContext;

        public async Task Handle(PlaceOpeningOrdersCommand request, CancellationToken cancellationToken)
        {
            var botsWithQuantities = await db.Bots
                .Where(b => b.Enabled)
                .Where(b =>
                    b.MaxPrice == null
                    || (b.IsLong && b.MaxPrice >= request.Ticker.Bid)
                    || (!b.IsLong && b.MaxPrice >= request.Ticker.Ask))
                .Where(b =>
                    b.MinPrice == null
                    || (b.IsLong && b.MinPrice <= request.Ticker.Ask)
                    || (!b.IsLong && b.MinPrice <= request.Ticker.Bid))
                .Where(b =>
                    !b.PlaceOrdersInAdvance
                    || (b.PlaceOrdersInAdvance
                    && b.Trades.Count(t => t.Profit == null) < b.MaxOrders))
                .Select(b => new
                {
                    Bot = b,
                    HasOpenTrades = b.Trades.Any(t => t.Profit == null),
                    FirstTradePrice = b.IsLong
                        ? b.Trades.Where(t => t.Profit == null).Max(t => t.EntryOrder.Price)
                        : b.Trades.Where(t => t.Profit == null).Min(t => t.EntryOrder.Price),
                    CurrentVolume = b.Trades.Where(t => t.Profit == null).Sum(t => t.EntryOrder.Quantity),
                    CurrentPrice = b.IsLong ? request.Ticker.Ask : request.Ticker.Bid
                })
                .Select(x => new
                {
                    x.Bot,
                    Quantity = !x.HasOpenTrades
                        ? x.Bot.EntryQuantity
                        : (int)((x.CurrentPrice - x.FirstTradePrice) / x.Bot.EntryStep * x.Bot.EntryQuantity) - x.CurrentVolume
                })
                .Where(x => x.Quantity > 0)
                .ToListAsync(cancellationToken);

            await Parallel.ForEachAsync(botsWithQuantities, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (botWithQuantity, token) =>
            {
                await PlaceOrder(botWithQuantity.Bot, request.Ticker, botWithQuantity.Quantity, token);
            });
        }

        private async Task PlaceOrder(Bot bot, Ticker ticker, decimal quantity, CancellationToken cancellationToken)
        {
            using var client = new BinanceRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(bot.PublicKey, bot.PrivateKey);
            });

            var order = await client.SpotApi.Trading.PlaceOrderAsync(
                symbol: ticker.Symbol,
                side: bot.IsLong ? OrderSide.Buy : OrderSide.Sell,
                type: SpotOrderType.Limit,
                quantity: quantity,
                price: bot.IsLong ? ticker.Ask : ticker.Bid,
                timeInForce: TimeInForce.GoodTillCanceled);

            if (order.Success)
            {
                var dbOrder = new Order(
                    id: 0,
                    symbol: ticker.Symbol,
                    price: order.Data.Price,
                    quantity: order.Data.Quantity,
                    isBuy: bot.IsLong,
                    createdAt: DateTime.UtcNow);

                var trade = new Trade(dbOrder);

                bot.Trades.Add(trade);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}