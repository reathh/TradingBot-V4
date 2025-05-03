using MediatR;
using TradingBot.Data;
using Microsoft.EntityFrameworkCore;
using TradingBot.Services;
using Microsoft.Extensions.Logging;

namespace TradingBot.Application;

public class PlaceOpeningOrdersCommand : IRequest
{
    public required Ticker Ticker { get; set; }

    public class PlaceOpeningOrdersCommandHandler(TradingBotDbContext dbContext, IExchangeApi exchangeApi, ILogger<PlaceOpeningOrdersCommandHandler> logger) : IRequestHandler<PlaceOpeningOrdersCommand>
    {
        private readonly TradingBotDbContext db = dbContext;
        private readonly IExchangeApi exchangeApi = exchangeApi;
        private readonly ILogger<PlaceOpeningOrdersCommandHandler> logger = logger;

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
                    && b.Trades.Count(t => t.Profit == null) < b.MaxOrdersInAdvance))
                .Select(b => new
                {
                    Bot = b,
                    FirstTradePrice = b.IsLong
                        ? b.Trades.Where(t => t.Profit == null).Max(t => t.EntryOrder.Price)
                        : b.Trades.Where(t => t.Profit == null).Min(t => t.EntryOrder.Price),
                    CurrentVolume = b.Trades.Where(t => t.Profit == null).Sum(t => t.EntryOrder.Quantity),
                    CurrentPrice = b.IsLong ? request.Ticker.Ask : request.Ticker.Bid,
                    OpenTradesCount = b.Trades.Count(t => t.Profit == null)
                })
                .Select(x => new
                {
                    x.Bot,
                    x.OpenTradesCount,
                    Quantity = x.OpenTradesCount == 0
                        ? x.Bot.EntryQuantity
                        : (int)((x.CurrentPrice - x.FirstTradePrice) / x.Bot.EntryStep * x.Bot.EntryQuantity) - x.CurrentVolume
                })
                .Where(x => x.Quantity > 0)
                .ToListAsync(cancellationToken);

            await Parallel.ForEachAsync(botsWithQuantities, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (botWithQuantity, token) =>
            {
                try
                {
                    await PlaceOrder(botWithQuantity.Bot, request.Ticker, botWithQuantity.Quantity, botWithQuantity.OpenTradesCount, token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to place orders for bot {BotId} with quantity {Quantity}", botWithQuantity.Bot.Id, botWithQuantity.Quantity);
                }
            });
        }

        private async Task PlaceOrder(Bot bot, Ticker ticker, decimal quantity, int openTradesCount, CancellationToken cancellationToken)
        {
            var currentPrice = bot.IsLong ? ticker.Ask : ticker.Bid;
            var stepDirection = bot.IsLong ? 1 : -1;

            var orderTasks = new List<Task<Order>>
            {
                exchangeApi.PlaceOrder(
                    bot,
                    bot.IsLong ? ticker.Bid : ticker.Ask,
                    quantity,
                    bot.IsLong,
                    cancellationToken)
            };

            if (bot.PlaceOrdersInAdvance)
            {
                var ordersToPlace = bot.MaxOrdersInAdvance - openTradesCount;

                if (ordersToPlace > 0)
                {
                    orderTasks.AddRange(Enumerable.Range(1, ordersToPlace)
                        .Select(i =>
                        {
                            var nextPrice = currentPrice + (bot.EntryStep * stepDirection * i);
                            return exchangeApi.PlaceOrder(
                                bot,
                                nextPrice,
                                bot.EntryQuantity,
                                bot.IsLong,
                                cancellationToken);
                        }));
                }
            }

            var orders = await Task.WhenAll(orderTasks);

            foreach (var order in orders)
            {
                var trade = new Trade(order);
                bot.Trades.Add(trade);
                logger.LogInformation(
                    "Bot {BotId} placed {Side} order at {Price} for {Quantity} units ({OrderId})",
                    bot.Id,
                    order.IsBuy ? "buy" : "sell",
                    order.Price,
                    order.Quantity,
                    order.ExchangeOrderId);
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully placed {OrderCount} entry orders for bot {BotId}", orders.Length, bot.Id);
        }
    }
}