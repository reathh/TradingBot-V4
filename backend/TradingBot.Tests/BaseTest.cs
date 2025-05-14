using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;
using TradingBot.Services;
using TradingBot.Tests.Helpers;

namespace TradingBot.Tests;

using Models;

public abstract class BaseTest
{
    protected readonly Mock<IExchangeApi> ExchangeApiMock;
    protected readonly Mock<IExchangeApiRepository> ExchangeApiRepositoryMock;
    protected readonly Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>> LoggerMock;
    protected readonly string DbName;
    protected readonly TestDbContextFactory DbContextFactory;
    protected readonly PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler Handler;

    private readonly Random _random = new();

    protected BaseTest()
    {
        ExchangeApiMock = new Mock<IExchangeApi>();
        ExchangeApiRepositoryMock = new Mock<IExchangeApiRepository>();
        LoggerMock = new Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>>();

        // Configure the repository mock to return the exchange API mock
        ExchangeApiRepositoryMock.Setup(x => x.GetExchangeApi(It.IsAny<Bot>()))
            .Returns(ExchangeApiMock.Object);

        DbName = Guid.NewGuid().ToString();
        DbContextFactory = new TestDbContextFactory(DbName);
        var notificationService = new TestTradingNotificationService();
        Handler = new PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler(
            DbContextFactory,
            ExchangeApiRepositoryMock.Object,
            notificationService,
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
            EntryOrdersInAdvance = ordersInAdvance,
            ExitOrdersInAdvance = ordersInAdvance,
            EntryQuantity = entryQuantity,
            EntryStep = entryStep,
            ExitStep = exitStep,
            Trades = []
        };

        using var context = DbContextFactory.CreateDbContext();
        context.Bots.Add(bot);
        await context.SaveChangesAsync();
        return bot;
    }

    protected TickerDto CreateTicker(decimal bid, decimal ask)
    {
        var lastPrice = _random.Next(2) == 0 ? bid : ask;
        var now = DateTime.UtcNow;
        return new TickerDto(
            "BTCUSDT", now, bid, ask, lastPrice, lastPrice, 
            lastPrice * 1.01m, lastPrice * 0.99m, 1000m, 
            1000m * lastPrice, lastPrice, 0m, 0m, 
            100, now.AddDays(-1), now);
    }

    protected Order CreateOrder(
        Bot bot, 
        decimal price, 
        decimal quantity, 
        bool isBuy, 
        OrderStatus status = OrderStatus.New, 
        decimal? quantityFilled = null,
        decimal? fee = null)
    {
        var order = new Order(Guid.NewGuid().ToString(), bot.Symbol, price, quantity, isBuy, DateTime.UtcNow)
        {
            Quantity = quantity,
            QuantityFilled = quantityFilled ?? quantity,
            AverageFillPrice = price,
            Fee = fee ?? 0m,
            Status = status
        };
        
        return order;
    }
}

public class TestTradingNotificationService : TradingBot.Services.TradingNotificationService
{
    public TestTradingNotificationService() : base(null, null) { }
    public new Task NotifyOrderUpdated(string orderId) => Task.CompletedTask;
}