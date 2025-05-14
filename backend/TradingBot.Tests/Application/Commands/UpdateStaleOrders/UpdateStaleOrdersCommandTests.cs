namespace TradingBot.Tests.Application.Commands.UpdateStaleOrders;

using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.UpdateStaleOrders;
using TradingBot.Services;
using TradingBot.Tests.Helpers;

public class UpdateStaleOrdersCommandTests : BaseTest
{
    private readonly UpdateStaleOrdersCommandHandler _handler;
    private readonly Mock<ILogger<UpdateStaleOrdersCommandHandler>> _loggerMock;
    private readonly Mock<IExchangeApiRepository> _exchangeApiRepositoryMock;
    private readonly Mock<IExchangeApi> _exchangeApiMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly string _dbName;
    private readonly TestDbContextFactory _dbContextFactory;
    private readonly DateTime _currentTime = new(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public UpdateStaleOrdersCommandTests()
    {
        _loggerMock = new Mock<ILogger<UpdateStaleOrdersCommandHandler>>();
        _exchangeApiMock = new Mock<IExchangeApi>();
        _exchangeApiRepositoryMock = new Mock<IExchangeApiRepository>();
        _timeProviderMock = new Mock<TimeProvider>();

        // Configure the repository mock to return the API mock
        _exchangeApiRepositoryMock
            .Setup(x => x.GetExchangeApi(It.IsAny<Bot>()))
            .Returns(_exchangeApiMock.Object);

        // Configure time provider to return a fixed time
        _timeProviderMock
            .Setup(x => x.GetUtcNow())
            .Returns(new DateTimeOffset(_currentTime));

        _dbName = Guid.NewGuid().ToString();
        _dbContextFactory = new TestDbContextFactory(_dbName);
        var notificationService = new TestTradingNotificationService();
        using var context = _dbContextFactory.CreateDbContext();
        _handler = new UpdateStaleOrdersCommandHandler(
            context,
            _exchangeApiRepositoryMock.Object,
            _timeProviderMock.Object,
            notificationService,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateStaleOrders_WhenOrdersExistAndAreStale()
    {
        // Arrange
        var bot = await CreateBot();

        // Create a stale order (updated more than 10 minutes ago)
        var staleOrder = CreateOrder(bot, 100m, 1m, true);
        staleOrder.LastUpdated = _currentTime.AddMinutes(-15);

        // Create a recently updated order (less than 10 minutes ago)
        var recentOrder = CreateOrder(bot, 200m, 2m, false);
        recentOrder.LastUpdated = _currentTime.AddMinutes(-5);

        // Create a closed order
        var closedOrder = CreateOrder(bot, 300m, 3m, true, OrderStatus.Filled);
        closedOrder.LastUpdated = _currentTime.AddMinutes(-20);

        // Add the orders to the database
        using (var context = _dbContextFactory.CreateDbContext())
        {
            context.Orders.AddRange(staleOrder, recentOrder, closedOrder);
            await context.SaveChangesAsync();
        }

        // Create trades for each order and link them to the bot
        var staleTrade = new Trade(staleOrder);
        var recentTrade = new Trade(recentOrder);
        var closedTrade = new Trade(closedOrder);

        // Add trades to bot
        bot.Trades.Add(staleTrade);
        bot.Trades.Add(recentTrade);
        bot.Trades.Add(closedTrade);

        // Add trades to database
        using (var context = _dbContextFactory.CreateDbContext())
        {
            context.Trades.AddRange(staleTrade, recentTrade, closedTrade);
            await context.SaveChangesAsync();
        }

        // Setup mock to return order status
        _exchangeApiMock
            .Setup(x => x.GetOrderStatus(staleOrder.Id, bot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderUpdate(
                Id: staleOrder.Id,
                Symbol: staleOrder.Symbol,
                Price: staleOrder.Price,
                Quantity: staleOrder.Quantity,
                QuantityFilled: staleOrder.Quantity * 0.5m, // Half filled
                AverageFillPrice: 101m, // Different fill price
                IsBuy: staleOrder.IsBuy,
                Status: OrderStatus.Filled,
                Fee: null));

        var command = new UpdateStaleOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data); // Only 1 order should be updated

        // Verify order was updated in database with data from exchange
        using (var context = _dbContextFactory.CreateDbContext())
        {
            var updatedOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == staleOrder.Id);
            Assert.NotNull(updatedOrder);
            Assert.Equal(_currentTime, updatedOrder.LastUpdated);
            Assert.Equal(staleOrder.Quantity * 0.5m, updatedOrder.QuantityFilled); // Verify quantity filled was updated
            Assert.Equal(101m, updatedOrder.AverageFillPrice); // Verify price was updated
        }

        // Verify other orders were not updated
        using (var context = _dbContextFactory.CreateDbContext())
        {
            var nonUpdatedOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == recentOrder.Id);
            Assert.NotNull(nonUpdatedOrder);
            Assert.Equal(_currentTime.AddMinutes(-5), nonUpdatedOrder.LastUpdated);

            var nonUpdatedClosedOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == closedOrder.Id);
            Assert.NotNull(nonUpdatedClosedOrder);
            Assert.Equal(_currentTime.AddMinutes(-20), nonUpdatedClosedOrder.LastUpdated);
        }
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenNoStaleOrdersExist()
    {
        // Arrange
        var command = new UpdateStaleOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Data);
    }

    [Fact]
    public async Task Handle_ShouldSkipOrdersWithNoTrade()
    {
        // Arrange
        // Create an order without attaching it to a bot or trade
        var order = new Order(
            id: Guid.NewGuid().ToString(),
            symbol: "BTCUSDT",
            price: 100m,
            quantity: 1m,
            isBuy: true,
            createdAt: DateTime.UtcNow
        );
        order.LastUpdated = _currentTime.AddMinutes(-15);

        using (var context = _dbContextFactory.CreateDbContext())
        {
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }

        var command = new UpdateStaleOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Data); // No orders should be updated since we skipped the one without a trade
    }

    [Fact]
    public async Task Handle_ShouldHandleExchangeErrors()
    {
        // Arrange
        var bot = await CreateBot();

        // Create a stale order
        var staleOrder = CreateOrder(bot, 100m, 1m, true);
        staleOrder.LastUpdated = _currentTime.AddMinutes(-15);

        // Add the order to the database
        using (var context = _dbContextFactory.CreateDbContext())
        {
            context.Orders.Add(staleOrder);
            await context.SaveChangesAsync();
        }

        // Create trade and link to bot
        var staleTrade = new Trade(staleOrder);
        bot.Trades.Add(staleTrade);

        // Add trade to database
        using (var context = _dbContextFactory.CreateDbContext())
        {
            context.Trades.Add(staleTrade);
            await context.SaveChangesAsync();
        }

        // Setup mock to throw an exception
        _exchangeApiMock
            .Setup(x => x.GetOrderStatus(staleOrder.Id, bot, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Exchange error"));

        var command = new UpdateStaleOrdersCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Data); // Should still succeed but with 0 orders updated

        // Verify order LastUpdated was still updated (to prevent continuous retries)
        using (var context = _dbContextFactory.CreateDbContext())
        {
            var updatedOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == staleOrder.Id);
            Assert.NotNull(updatedOrder);
            Assert.Equal(_currentTime, updatedOrder.LastUpdated);
        }

        // Verify the error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o != null && o.ToString()!.Contains("Failed to get status for order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

public class TestTradingNotificationService : TradingBot.Services.TradingNotificationService
{
    public TestTradingNotificationService() : base(null, null) { }
    public new Task NotifyOrderUpdated(string orderId) => Task.CompletedTask;
}