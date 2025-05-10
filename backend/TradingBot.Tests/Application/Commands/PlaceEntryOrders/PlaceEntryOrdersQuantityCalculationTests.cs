using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Tests for quantity calculation in entry order placement
/// </summary>
public class PlaceEntryOrdersQuantityCalculationTests
{
    private readonly Mock<IExchangeApi> _exchangeApiMock;
    private readonly Mock<IExchangeApiRepository> _exchangeApiRepositoryMock;
    private readonly Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>> _loggerMock;
    private readonly TradingBotDbContext _dbContext;
    private readonly PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler _handler;

    private readonly Random _random = new();
    private int _nextBotId = 1;

    public PlaceEntryOrdersQuantityCalculationTests()
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
    public async Task Handle_ShouldCalculateCorrectQuantity_WhenPriceMovesUpAndDown_LongBot()
    {
        // Arrange
        var bot = await CreateBot(entryQuantity: 1, entryStep: 1m, isLong: true);

        // First ticker: 100/101
        var firstTicker = CreateTicker(100, 101);
        var firstCommand = new PlaceEntryOrdersCommand { Ticker = firstTicker };

        // Place initial order at 100 (Bid price since we're long)
        var firstOrder = CreateOrder(bot, 100, 1, bot.IsLong);
        _exchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstOrder);

        // Act - Place first order
        await _handler.Handle(firstCommand, CancellationToken.None);

        // Assert - Verify first order
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100),
            It.Is<decimal>(q => q == 1),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        // Clear mock for next test
        _exchangeApiMock.Reset();

        // Second ticker: 95/96 (price dropped)
        var secondTicker = CreateTicker(95, 96);
        var secondCommand = new PlaceEntryOrdersCommand { Ticker = secondTicker };

        // We need orders at 99,98,97,96,95 (5 units)
        // We already have an order at 100
        var secondOrder = CreateOrder(bot, 95, 5, bot.IsLong);
        _exchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondOrder);

        // Act - Place second order
        await _handler.Handle(secondCommand, CancellationToken.None);

        // Assert - Verify second order
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 95),
            It.Is<decimal>(q => q == 5),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCalculateCorrectQuantity_WhenPriceMovesUpAndDown_ShortBot()
    {
        // Arrange
        var bot = await CreateBot(entryQuantity: 1, entryStep: 1m, isLong: false);

        // First ticker: 100/101
        var firstTicker = CreateTicker(100, 101);
        var firstCommand = new PlaceEntryOrdersCommand { Ticker = firstTicker };

        // Place initial order at 101 (Ask price since we're short)
        var firstOrder = CreateOrder(bot, 101, 1, bot.IsLong);
        _exchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstOrder);

        // Act - Place first order
        await _handler.Handle(firstCommand, CancellationToken.None);

        // Assert - Verify first order
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 101),
            It.Is<decimal>(q => q == 1),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        // Clear mock for next test
        _exchangeApiMock.Reset();

        // Second ticker: 105/106 (price rose)
        var secondTicker = CreateTicker(105, 106);
        var secondCommand = new PlaceEntryOrdersCommand { Ticker = secondTicker };

        // We need orders at 102,103,104,105,106 (5 units)
        // We already have an order at 101
        var secondOrder = CreateOrder(bot, 106, 5, bot.IsLong);
        _exchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondOrder);

        // Act - Place second order
        await _handler.Handle(secondCommand, CancellationToken.None);

        // Assert - Verify second order
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 106),
            It.Is<decimal>(q => q == 5),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenCalculatedQuantityIsZero()
    {
        // Arrange
        var bot = await CreateBot(entryStep: 1.0m);

        // Create an initial trade
        var existingOrder = CreateOrder(bot, 100, bot.EntryQuantity, bot.IsLong);
        var existingTrade = new Trade(existingOrder);
        bot.Trades.Add(existingTrade);
        await _dbContext.SaveChangesAsync();

        // Create a ticker with price very close to existing order to generate 0 quantity
        // The price movement is less than entryStep, so no new orders should be placed
        var ticker = CreateTicker(99.9m, 100.2m);
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPlaceCorrectOrders_WhenEntryStepAndQuantityAreChanged()
    {
        // Arrange
        var bot = await CreateBot(entryQuantity: 1, entryStep: 1m, isLong: true);

        // First ticker: 100/101
        var firstTicker = CreateTicker(100, 101);
        var firstCommand = new PlaceEntryOrdersCommand { Ticker = firstTicker };

        // Place initial order at 100 (Bid price since we're long)
        var firstOrder = CreateOrder(bot, 100, 1, bot.IsLong);
        _exchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstOrder);

        // Act - Place first order
        await _handler.Handle(firstCommand, CancellationToken.None);

        // Now change the bot's entryStep and entryQuantity
        bot.EntryStep = 2m;     // Larger step - should place fewer orders
        bot.EntryQuantity = 3m; // Larger quantity - each order is larger
        await _dbContext.SaveChangesAsync();

        // Clear mock for next test
        _exchangeApiMock.Reset();

        // Second ticker: 94/95 (price dropped)
        var secondTicker = CreateTicker(94, 95);
        var secondCommand = new PlaceEntryOrdersCommand { Ticker = secondTicker };

        // With new entry step of 2, we need orders at:
        // 98, 96, 94 (with first at 100, that's 3 price levels from 100 to 94)
        // Total calculation: (100-94)/2 + 1 = 4 price levels
        // 4 price levels × 3 units = 12 total units needed
        // We already have 1 unit at 100, so need 11 more units
        var secondOrder = CreateOrder(bot, 94, 11, bot.IsLong);
        _exchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondOrder);

        // Act - Place second order with updated bot parameters
        await _handler.Handle(secondCommand, CancellationToken.None);

        // Assert - Verify second order with updated parameters
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 94),
            It.Is<decimal>(q => q == 11), // Correct quantity based on calculations
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
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

    private TickerDto CreateTicker(decimal bid, decimal ask) => 
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