using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Tests for advance entry order placement functionality
/// </summary>
public class PlaceEntryOrdersAdvanceTests
{
    private readonly Mock<IExchangeApi> _exchangeApiMock;
    private readonly Mock<IExchangeApiRepository> _exchangeApiRepositoryMock;
    private readonly Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>> _loggerMock;
    private readonly TradingBotDbContext _dbContext;
    private readonly PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler _handler;

    private readonly Random _random = new();
    private int _nextBotId = 1;

    public PlaceEntryOrdersAdvanceTests()
    {
        _exchangeApiMock = new Mock<IExchangeApi>();
        _exchangeApiRepositoryMock = new Mock<IExchangeApiRepository>();
        _loggerMock = new Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>>();

        // Configure the repository mock to return the exchange API mock
        _exchangeApiRepositoryMock.Setup(x => x.GetExchangeApi(It.IsAny<Bot>()))
            .Returns(_exchangeApiMock.Object);

        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TradingBotDbContext(options);
        _handler = new PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler(
            _dbContext,
            _exchangeApiRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldPlaceOrdersInAdvance_WhenConfigured_LongBot()
    {
        // Arrange
        var bot = await CreateBot(
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            entryStep: 1m);
        var ticker = CreateTicker(100, 101);
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        var orders = new[]
        {
            CreateOrder(bot, bot.IsLong ? ticker.Bid : ticker.Ask, bot.EntryQuantity, bot.IsLong),
            CreateOrder(bot, bot.IsLong ? ticker.Bid - 1 : ticker.Ask + 1, bot.EntryQuantity, bot.IsLong),
            CreateOrder(bot, bot.IsLong ? ticker.Bid - 2 : ticker.Ask + 2, bot.EntryQuantity, bot.IsLong)
        };

        _exchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders[0])
            .ReturnsAsync(orders[1])
            .ReturnsAsync(orders[2]);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));

        var savedTrades = await _dbContext.Trades.ToListAsync();
        Assert.Equal(3, savedTrades.Count);
    }

    [Fact]
    public async Task Handle_ShouldPlaceOrdersInAdvance_WhenConfigured_ShortBot()
    {
        // Arrange
        var bot = await CreateBot(
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            entryStep: 1m,
            isLong: false);
        var ticker = CreateTicker(100, 101);
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        var orders = new[]
        {
            CreateOrder(bot, 101, bot.EntryQuantity, bot.IsLong), // Ask price
            CreateOrder(bot, 102, bot.EntryQuantity, bot.IsLong), // Ask + 1
            CreateOrder(bot, 103, bot.EntryQuantity, bot.IsLong)  // Ask + 2
        };

        _exchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders[0])
            .ReturnsAsync(orders[1])
            .ReturnsAsync(orders[2]);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));

        var savedTrades = await _dbContext.Trades.ToListAsync();
        Assert.Equal(3, savedTrades.Count);
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenOrdersInAdvanceEqualsOpenTrades()
    {
        // Arrange
        var bot = await CreateBot(placeOrdersInAdvance: true, ordersInAdvance: 1);
        var ticker = CreateTicker(100, 101);

        // Place one order first
        var firstOrder = CreateOrder(bot, bot.IsLong ? ticker.Bid : ticker.Ask, bot.EntryQuantity, bot.IsLong);
        _exchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstOrder);

        var command = new PlaceEntryOrdersCommand { Ticker = ticker };
        await _handler.Handle(command, CancellationToken.None);

        // Reset mock for second attempt
        _exchangeApiMock.Reset();

        // Act - Try to place another order
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Verify no additional orders were placed
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task<Bot> CreateBot(
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

        _dbContext.Bots.Add(bot);
        await _dbContext.SaveChangesAsync();
        return bot;
    }

    private Ticker CreateTicker(decimal bid, decimal ask) => 
        new("BTCUSDT", DateTime.UtcNow, bid, ask, lastPrice: _random.Next(2) == 0 ? bid : ask);

    private Order CreateOrder(Bot bot, decimal price, decimal quantity, bool isBuy)
    {
        return new Order(Guid.NewGuid().ToString(), bot.Symbol, price, quantity, isBuy, DateTime.UtcNow)
        {
            Quantity = quantity,
            QuantityFilled = quantity,
            AverageFillPrice = price,
            Fees = 0.001m * price * quantity
        };
    }
} 