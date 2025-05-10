using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Tests for price boundary conditions in entry order placement
/// </summary>
public class PlaceEntryOrdersPriceBoundaryTests
{
    private readonly Mock<IExchangeApi> _exchangeApiMock;
    private readonly Mock<IExchangeApiRepository> _exchangeApiRepositoryMock;
    private readonly Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>> _loggerMock;
    private readonly TradingBotDbContext _dbContext;
    private readonly PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler _handler;

    private readonly Random _random = new();
    private int _nextBotId = 1;

    public PlaceEntryOrdersPriceBoundaryTests()
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
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceIsAboveMaxPrice()
    {
        // Arrange
        var bot = await CreateBot(maxPrice: 100);
        var ticker = CreateTicker(101, 102);
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
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceIsBelowMinPrice()
    {
        // Arrange
        var bot = await CreateBot(minPrice: 100);
        var ticker = CreateTicker(98, 99);
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
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceIsBelowMinPrice_ShortBot()
    {
        // Arrange
        var bot = await CreateBot(minPrice: 100, isLong: false);
        var ticker = CreateTicker(98, 99);  // Both bid and ask are below MinPrice
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
    public async Task Handle_ShouldPlaceOrder_WhenPriceIsExactlyAtMinPrice()
    {
        // Arrange
        var minPrice = 100m;
        var bot = await CreateBot(minPrice: minPrice);
        var ticker = CreateTicker(minPrice - 1, minPrice); // Ask is exactly at MinPrice
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        var expectedOrder = CreateOrder(bot, bot.IsLong ? ticker.Bid : ticker.Ask, bot.EntryQuantity, bot.IsLong);
        _exchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Order should be placed
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPlaceOrder_WhenPriceIsExactlyAtMaxPrice()
    {
        // Arrange
        var maxPrice = 100m;
        var bot = await CreateBot(maxPrice: maxPrice);
        var ticker = CreateTicker(maxPrice, maxPrice + 1); // Bid is exactly at MaxPrice
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        var expectedOrder = CreateOrder(bot, bot.IsLong ? ticker.Bid : ticker.Ask, bot.EntryQuantity, bot.IsLong);
        _exchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Order should be placed
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
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