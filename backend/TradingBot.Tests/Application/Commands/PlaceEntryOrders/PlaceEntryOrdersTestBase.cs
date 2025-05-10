using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Base class for entry order tests providing common functionality
/// </summary>
public abstract class PlaceEntryOrdersTestBase
{
    protected readonly Mock<IExchangeApi> ExchangeApiMock;
    protected readonly Mock<IExchangeApiRepository> ExchangeApiRepositoryMock;
    protected readonly Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>> LoggerMock;
    protected readonly TradingBotDbContext DbContext;
    protected readonly PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler Handler;

    private readonly Random _random = new();
    private int _nextBotId = 1;

    protected PlaceEntryOrdersTestBase()
    {
        ExchangeApiMock = new Mock<IExchangeApi>();
        ExchangeApiRepositoryMock = new Mock<IExchangeApiRepository>();
        LoggerMock = new Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>>();

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

    protected Ticker CreateTestTicker(decimal bid, decimal ask) => 
        new("BTCUSDT", DateTime.UtcNow, bid, ask, lastPrice: _random.Next(2) == 0 ? bid : ask);

    protected Order CreateOrder(Bot bot, decimal price, decimal quantity, bool isBuy)
    {
        return new Order(Guid.NewGuid().ToString(), bot.Symbol, price, quantity, isBuy, DateTime.UtcNow)
        {
            Quantity = quantity,
            QuantityFilled = quantity,
            AverageFillPrice = price,
            Fees = 0.001m * price * quantity
        };
    }

    protected async Task<Trade> CreateTrade(Bot bot, decimal entryPrice, bool closed = true)
    {
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        entryOrder.Closed = closed;
        
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();
        return trade;
    }

    protected Task<Result> HandleCommand(PlaceEntryOrdersCommand command, CancellationToken cancellationToken)
    {
        return Handler.Handle(command, cancellationToken);
    }
} 