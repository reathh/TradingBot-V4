using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Application.Commands.PlaceOpeningOrders;

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
                // Price range filters
                .Where(b =>
                    (b.MaxPrice == null || (b.IsLong && b.MaxPrice >= request.Ticker.Bid) || (!b.IsLong && b.MaxPrice >= request.Ticker.Ask)) &&
                    (b.MinPrice == null || (b.IsLong && b.MinPrice <= request.Ticker.Ask) || (!b.IsLong && b.MinPrice <= request.Ticker.Bid)))
                // Orders in advance filter
                .Where(b => !b.PlaceOrdersInAdvance || (b.PlaceOrdersInAdvance && b.Trades.Count(t => t.Profit == null) < b.OrdersInAdvance))
                // Select all data needed for calculation
                .Select(b => new
                {
                    Bot = b,
                    OpenTradesCount = b.Trades.Count(t => t.Profit == null),
                    HasOpenTrades = b.Trades.Any(t => t.Profit == null),
                    // Get the price of the first trade (chronologically) to use as reference
                    FirstTradePrice = b.Trades
                        .Where(t => t.Profit == null)
                        .OrderBy(t => t.EntryOrder.CreatedAt)
                        .Select(t => t.EntryOrder.Price)
                        .FirstOrDefault(),
                    // Total volume already allocated
                    CurrentVolume = b.Trades.Where(t => t.Profit == null).Sum(t => t.EntryOrder.Quantity),
                    // Current price to place order at
                    CurrentPrice = b.IsLong ? request.Ticker.Bid : request.Ticker.Ask
                })
                // Calculate needed quantity in a single step
                .Select(x => new
                {
                    x.Bot,
                    x.OpenTradesCount,
                    Quantity = !x.HasOpenTrades
                        // First order case
                        ? x.Bot.EntryQuantity
                        // For existing positions, calculate required quantity based on price movement
                        : x.Bot.IsLong
                            // Long positions: calculate difference when price moves down
                            ? Math.Max(0, ((int)Math.Ceiling(Math.Abs(x.FirstTradePrice - x.CurrentPrice) / x.Bot.EntryStep) + 1) * x.Bot.EntryQuantity - x.CurrentVolume)
                            // Short positions: calculate difference when price moves up
                            : Math.Max(0, ((int)Math.Ceiling(Math.Abs(x.CurrentPrice - x.FirstTradePrice) / x.Bot.EntryStep) + 1) * x.Bot.EntryQuantity - x.CurrentVolume)
                })
                // Only include bots that need to place orders
                .Where(x => x.Quantity > 0)
                .ToListAsync(cancellationToken);

            await Parallel.ForEachAsync(botsWithQuantities,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                async (botWithQuantity, token) =>
                {
                    try
                    {
                        await PlaceOrders(botWithQuantity.Bot, request.Ticker, botWithQuantity.Quantity, botWithQuantity.OpenTradesCount, token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to place orders for bot {BotId} with quantity {Quantity}",
                            botWithQuantity.Bot.Id, botWithQuantity.Quantity);
                    }
                });
        }

        private async Task PlaceOrders(Bot bot, Ticker ticker, decimal quantity, int openTradesCount, CancellationToken cancellationToken)
        {
            var currentPrice = bot.IsLong ? ticker.Bid : ticker.Ask;
            var stepDirection = bot.IsLong ? 1 : -1;

            if (bot.PlaceOrdersInAdvance)
            {
                var ordersToPlace = bot.OrdersInAdvance - openTradesCount;

                if (ordersToPlace <= 0)
                {
                    return;
                }

                var orderTasks = new List<Task<Order>>();

                for (int i = 0; i < ordersToPlace; i++)
                {
                    var orderPrice = currentPrice + (bot.EntryStep * stepDirection * i);
                    var orderQuantity = i == 0 ? quantity : bot.EntryQuantity;

                    orderTasks.Add(exchangeApi.PlaceOrder(
                        bot,
                        orderPrice,
                        orderQuantity,
                        bot.IsLong,
                        cancellationToken));
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
            else
            {
                // Standard behavior for bots without PlaceOrdersInAdvance
                var order = await exchangeApi.PlaceOrder(
                    bot,
                    currentPrice,
                    quantity,
                    bot.IsLong,
                    cancellationToken);

                var trade = new Trade(order);
                bot.Trades.Add(trade);
                logger.LogInformation(
                    "Bot {BotId} placed {Side} order at {Price} for {Quantity} units ({OrderId})",
                    bot.Id,
                    order.IsBuy ? "buy" : "sell",
                    order.Price,
                    order.Quantity,
                    order.ExchangeOrderId);

                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Successfully placed 1 entry order for bot {BotId}", bot.Id);
            }
        }
    }
}