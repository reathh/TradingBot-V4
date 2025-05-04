using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceOpeningOrders;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests;

public abstract class BaseTest
{
    protected readonly Mock<IExchangeApi> ExchangeApiMock;
    protected readonly Mock<ILogger<PlaceOpeningOrdersCommand.PlaceOpeningOrdersCommandHandler>> LoggerMock;
    protected readonly TradingBotDbContext DbContext;
    protected readonly PlaceOpeningOrdersCommand.PlaceOpeningOrdersCommandHandler Handler;

    private readonly Random _random = new();

    protected BaseTest()
    {
        ExchangeApiMock = new Mock<IExchangeApi>();
        LoggerMock = new Mock<ILogger<PlaceOpeningOrdersCommand.PlaceOpeningOrdersCommandHandler>>();

        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new TradingBotDbContext(options);
        Handler = new PlaceOpeningOrdersCommand.PlaceOpeningOrdersCommandHandler(
            DbContext,
            ExchangeApiMock.Object,
            LoggerMock.Object);
    }

    protected async Task<Bot> CreateBotAsync(
        bool isLong = true,
        decimal? maxPrice = null,
        decimal? minPrice = null,
        bool placeOrdersInAdvance = false,
        int ordersInAdvance = 0,
        decimal entryQuantity = 1,
        decimal entryStep = 0.1m)
    {
        var bot = new Bot(1, "TestBot", "BTCUSDT", "Test bot for unit tests")
        {
            Enabled = true,
            IsLong = isLong,
            MaxPrice = maxPrice,
            MinPrice = minPrice,
            PlaceOrdersInAdvance = placeOrdersInAdvance,
            OrdersInAdvance = ordersInAdvance,
            EntryQuantity = entryQuantity,
            EntryStep = entryStep,
            Trades = new HashSet<Trade>()
        };

        DbContext.Bots.Add(bot);
        await DbContext.SaveChangesAsync();
        return bot;
    }

    protected Ticker CreateTicker(decimal bid, decimal ask) => new("BTCUSDT", DateTime.UtcNow, bid, ask, lastPrice: _random.Next(2) == 0 ? bid : ask);

    protected Order CreateOrder(Bot bot, decimal price, decimal quantity, bool isBuy)
    {
        return new Order(bot.Symbol, price, quantity, isBuy, DateTime.UtcNow)
        {
            ExchangeOrderId = Guid.NewGuid().ToString()
        };
    }
}