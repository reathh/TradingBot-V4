using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.UpdateBotOrder;
using TradingBot.Data;
using Xunit;

namespace TradingBot.Tests;

public class UpdateBotOrderCommandTests : BaseTest
{
    private readonly UpdateBotOrderCommandHandler _handler;
    private readonly Mock<ILogger<UpdateBotOrderCommandHandler>> _loggerMock;

    public UpdateBotOrderCommandTests()
    {
        _loggerMock = new Mock<ILogger<UpdateBotOrderCommandHandler>>();
        _handler = new UpdateBotOrderCommandHandler(DbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateExistingOrder_WhenValidOrderUpdateReceived()
    {
        // Arrange
        var bot = await CreateBot();
        var order = CreateOrder(bot, 100m, 1m, true);
        order.QuantityFilled = 0; // Initial state: not filled
        order.Closed = false;     // Initial state: not closed

        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();

        var orderUpdate = new OrderUpdate(
            Id: order.Id,
            Symbol: order.Symbol,
            Price: order.Price,
            Quantity: order.Quantity,
            QuantityFilled: order.Quantity, // Update: fully filled
            AverageFillPrice: 100.5m,       // Update: different fill price
            IsBuy: order.IsBuy,
            Canceled: false,
            Closed: true                    // Update: now closed
        );

        var command = new UpdateBotOrderCommand(orderUpdate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify order was updated in database
        var updatedOrder = await DbContext.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal(orderUpdate.QuantityFilled, updatedOrder.QuantityFilled);
        Assert.Equal(orderUpdate.AverageFillPrice, updatedOrder.AverageFillPrice);
        Assert.Equal(orderUpdate.Closed, updatedOrder.Closed);
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
            IsBuy: true,
            Canceled: false,
            Closed: true
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
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(nonExistentOrderId)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFindOrderByExchangeOrderId_WhenIdDoesNotMatch()
    {
        // Arrange
        var bot = await CreateBot();
        var order = CreateOrder(bot, 100m, 1m, true);
        order.Id = "internal-id";
        order.ExchangeOrderId = "exchange-id";
        order.QuantityFilled = 0;
        order.Closed = false;

        DbContext.Orders.Add(order);
        await DbContext.SaveChangesAsync();

        var orderUpdate = new OrderUpdate(
            Id: "exchange-id",  // This matches ExchangeOrderId, not Id
            Symbol: order.Symbol,
            Price: order.Price,
            Quantity: order.Quantity,
            QuantityFilled: order.Quantity,
            AverageFillPrice: 100.5m,
            IsBuy: order.IsBuy,
            Canceled: false,
            Closed: true
        );

        var command = new UpdateBotOrderCommand(orderUpdate);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify order was updated in database
        var updatedOrder = await DbContext.Orders.FirstOrDefaultAsync(o => o.Id == "internal-id");
        Assert.NotNull(updatedOrder);
        Assert.Equal(orderUpdate.QuantityFilled, updatedOrder.QuantityFilled);
        Assert.Equal(orderUpdate.Closed, updatedOrder.Closed);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenExceptionOccurs()
    {
        // Arrange
        var orderUpdate = new OrderUpdate(
            Id: "test-id",
            Symbol: "BTCUSDT",
            Price: 100m,
            Quantity: 1m,
            QuantityFilled: 1m,
            AverageFillPrice: 100m,
            IsBuy: true,
            Canceled: false,
            Closed: true
        );

        var command = new UpdateBotOrderCommand(orderUpdate);

        // Create a mock DbContext that throws an exception on SaveChanges
        var mockDbContext = new Mock<TradingBotDbContext>(new DbContextOptions<TradingBotDbContext>());
        mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Create handler with the mock DbContext
        var handler = new UpdateBotOrderCommandHandler(mockDbContext.Object, _loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}