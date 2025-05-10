using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Tests for basic entry order placement functionality
/// </summary>
public class PlaceEntryOrdersBasicTests
{
    private readonly Mock<IExchangeApi> _exchangeApiMock;
    private readonly Mock<IExchangeApiRepository> _exchangeApiRepositoryMock;
    private readonly Mock<ILogger<PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler>> _loggerMock;
    private readonly TradingBotDbContext _dbContext;
    private readonly PlaceEntryOrdersCommand.PlaceEntryOrdersCommandHandler _handler;

    private readonly Random _random = new();
    private int _nextBotId = 1;

    public PlaceEntryOrdersBasicTests()
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
    public async Task Handle_ShouldPlaceOrder_WhenBotIsEnabledAndNoOpenTrades()
    {
        // Arrange
        var bot = await CreateBot();
        var ticker = CreateTicker(100, 101);
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

        // Assert
        _exchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == (bot.IsLong ? ticker.Bid : ticker.Ask)),
            It.Is<decimal>(q => q == bot.EntryQuantity),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        var savedTrade = await _dbContext.Trades.FirstOrDefaultAsync();
        Assert.NotNull(savedTrade);
        Assert.Equal(expectedOrder.Price, savedTrade.EntryOrder.Price);
        Assert.Equal(expectedOrder.Quantity, savedTrade.EntryOrder.Quantity);
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenBotIsDisabled()
    {
        // Arrange
        var bot = await CreateBot();
        bot.Enabled = false;
        await _dbContext.SaveChangesAsync();

        var ticker = CreateTicker(100, 101);
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