namespace TradingBot.Tests.Application.Commands.UpdateBotOrder;

using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.UpdateBotOrder;

public class UpdateBotOrderCommandTests : BaseTest
{
    private readonly UpdateBotOrderCommandHandler _handler;
    private readonly Mock<ILogger<UpdateBotOrderCommandHandler>> _loggerMock;
    private readonly Mock<TimeProvider> _timeProviderMock;

    public UpdateBotOrderCommandTests()
    {
        _loggerMock = new Mock<ILogger<UpdateBotOrderCommandHandler>>();
        _timeProviderMock = new Mock<TimeProvider>();

        // Configure time provider to return a fixed time
        _timeProviderMock
            .Setup(x => x.GetUtcNow())
            .Returns(new DateTimeOffset(DateTime.UtcNow));

        _handler = new UpdateBotOrderCommandHandler(DbContext, _loggerMock.Object, _timeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingOrder_WhenValidOrderUpdateReceived()
    {
        // Arrange
        var bot = await CreateBot();
        var order = CreateOrder(bot, 100m, 1m, true, OrderStatus.New, 0); // Initial state: not filled, not closed
        
        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();

        // Create OrderUpdate indicating filled status
        var orderUpdate = new OrderUpdate(
            Id: order.Id,
            Symbol: order.Symbol,
            Price: order.Price,
            Quantity: order.Quantity,
            QuantityFilled: order.Quantity, // Update: fully filled
            AverageFillPrice: 100.5m,       // Update: different fill price
            Fee: null,
            IsBuy: order.IsBuy,
            Status: OrderStatus.Filled      // Update: now filled (closed)
        );

        var command = new UpdateBotOrderCommand(orderUpdate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify order was updated in database
        var updatedOrder = await DbContext.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal(orderUpdate.QuantityFilled, updatedOrder!.QuantityFilled);
        Assert.Equal(orderUpdate.AverageFillPrice, updatedOrder.AverageFillPrice);
        Assert.Equal(orderUpdate.Status, updatedOrder.Status);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenOrderNotFound()
    {
        // Arrange
        var nonExistentOrderId = "non-existent-id";
        var orderUpdate = new OrderUpdate(
            Id: nonExistentOrderId,
            Symbol: "BTCUSDT",
            Price: 100m,
            Quantity: 1m,
            QuantityFilled: 1m,
            AverageFillPrice: 100m,
            Fee: null,
            IsBuy: true,
            Status: OrderStatus.Filled
        );

        var command = new UpdateBotOrderCommand(orderUpdate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o != null && o.ToString()!.Contains(nonExistentOrderId)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFindOrderById_WhenOrderExists()
    {
        // Arrange
        var bot = await CreateBot();
        var order = CreateOrder(bot, 100m, 1m, true, OrderStatus.New, 0);
        order.Id = "order-id";
        
        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();

        var orderUpdate = new OrderUpdate(
            Id: "order-id",  // This matches the order Id
            Symbol: order.Symbol,
            Price: order.Price,
            Quantity: order.Quantity,
            QuantityFilled: order.Quantity,
            AverageFillPrice: 100.5m,
            Fee: null,
            IsBuy: order.IsBuy,
            Status: OrderStatus.Filled
        );

        var command = new UpdateBotOrderCommand(orderUpdate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify order was updated in database
        var updatedOrder = await DbContext.Orders.FirstOrDefaultAsync(o => o.Id == "order-id");
        Assert.NotNull(updatedOrder);
        Assert.Equal(orderUpdate.QuantityFilled, updatedOrder!.QuantityFilled);
        Assert.Equal(orderUpdate.Status, updatedOrder.Status);
    }
}