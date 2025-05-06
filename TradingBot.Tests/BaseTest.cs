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
    protected readonly Mock<IExchangeApiRepository> ExchangeApiRepositoryMock;
    protected readonly Mock<ILogger<PlaceOpeningOrdersCommand.PlaceOpeningOrdersCommandHandler>> LoggerMock;
    protected readonly TradingBotDbContext DbContext;
    protected readonly PlaceOpeningOrdersCommand.PlaceOpeningOrdersCommandHandler Handler;

    private readonly Random _random = new();

    protected BaseTest()
    {
        ExchangeApiMock = new Mock<IExchangeApi>();
        ExchangeApiRepositoryMock = new Mock<IExchangeApiRepository>();
        LoggerMock = new Mock<ILogger<PlaceOpeningOrdersCommand.PlaceOpeningOrdersCommandHandler>>();

        // Configure the repository mock to return the exchange API mock
        ExchangeApiRepositoryMock.Setup(x => x.GetExchangeApi(It.IsAny<Bot>()))
            .Returns(ExchangeApiMock.Object);

        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new TradingBotDbContext(options);
        Handler = new PlaceOpeningOrdersCommand.PlaceOpeningOrdersCommandHandler(
            DbContext,
            ExchangeApiRepositoryMock.Object,
            LoggerMock.Object);
    }

    private int _nextBotId = 1;

    protected async Task<Bot> CreateBot(
        bool isLong = true,
        decimal? maxPrice = null,
        decimal? minPrice = null,
        bool placeOrdersInAdvance = false,
        int ordersInAdvance = 0,
        decimal entryQuantity = 1,
        decimal entryStep = 0.1m,
        decimal exitStep = 0.1m)
    {
        var botId = _nextBotId++;
        var bot = new Bot(botId, "TestBot" + botId, "public_key_" + botId, "private_key_" + botId)
        {
            Symbol = "BTCUSDT",
            Enabled = true,
            IsLong = isLong,
            MaxPrice = maxPrice,
            MinPrice = minPrice,
            PlaceOrdersInAdvance = placeOrdersInAdvance,
            OrdersInAdvance = ordersInAdvance,
            EntryQuantity = entryQuantity,
            EntryStep = entryStep,
            ExitStep = exitStep,
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
            Id = Guid.NewGuid().ToString(),
            ExchangeOrderId = Guid.NewGuid().ToString(),
            Quantity = quantity,
            QuantityFilled = quantity,
            AverageFillPrice = price,
            Fees = 0.001m * price * quantity,
            Closed = true
        };
    }
}