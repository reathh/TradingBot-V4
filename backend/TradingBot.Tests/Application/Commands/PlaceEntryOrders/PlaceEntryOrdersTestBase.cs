using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

using Models;

/// <summary>
/// Base class for entry order tests providing common functionality
/// </summary>
public abstract class PlaceEntryOrdersTestBase
{
    protected readonly Mock<IExchangeApi> ExchangeApiMock;
    protected readonly Mock<IExchangeApiRepository> ExchangeApiRepositoryMock;
    protected readonly Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>> LoggerMock;
    protected readonly TradingNotificationService NotificationServiceStub;
    protected readonly TradingBotDbContext DbContext;
    protected readonly PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler Handler;

    private readonly Random _random = new();
    private int _nextBotId = 1;

    protected PlaceEntryOrdersTestBase()
    {
        ExchangeApiMock = new Mock<IExchangeApi>();
        ExchangeApiRepositoryMock = new Mock<IExchangeApiRepository>();
        LoggerMock = new Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>>();
        NotificationServiceStub = new TestTradingNotificationService();

        // Configure the repository mock to return the exchange API mock
        ExchangeApiRepositoryMock.Setup(x => x.GetExchangeApi(It.IsAny<Bot>()))
            .Returns(ExchangeApiMock.Object);

        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new TradingBotDbContext(options);
        Handler = new PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler(
            DbContext,
            ExchangeApiRepositoryMock.Object,
            NotificationServiceStub,
            LoggerMock.Object);
    }
    
    protected async Task<Bot> CreateTestBot(
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

        DbContext.Bots.Add(bot);
        await DbContext.SaveChangesAsync();
        return bot;
    }

    protected TickerDto CreateTestTicker(decimal bid, decimal ask)
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
        decimal? quantityFilled = null)
    {
        var order = new Order(Guid.NewGuid().ToString(), bot.Symbol, price, quantity, isBuy, DateTime.UtcNow)
        {
            Quantity = quantity,
            QuantityFilled = quantityFilled ?? quantity,
            AverageFillPrice = price,
            Fee = 0.001m * price * quantity,
            Status = status
        };
        
        return order;
    }

    protected async Task<Trade> CreateTrade(Bot bot, decimal entryPrice, bool closed = true)
    {
        var status = closed ? OrderStatus.Filled : OrderStatus.New;
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong, status);
        
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();
        return trade;
    }

    protected Task<Result> HandleCommand(PlaceEntryOrdersCommand command, CancellationToken cancellationToken)
    {
        return Handler.Handle(command, cancellationToken);
    }

    private class TestTradingNotificationService : TradingNotificationService
    {
        public TestTradingNotificationService() : base(null, null) { }
        public Task NotifyOrderUpdated(string orderId) => Task.CompletedTask;
    }
} 