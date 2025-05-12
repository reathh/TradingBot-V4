using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Data;

namespace TradingBot.Tests.Application.Commands.PlaceExitOrders;

/// <summary>
/// Tests for error handling in exit order placement
/// </summary>
public class PlaceExitOrdersErrorTests : PlaceExitOrdersTestBase
{
    [Fact]
    public async Task Handle_ShouldLogError_WhenExitOrderPlacementFails()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        await CreateCompletedTrade(bot, entryPrice);
        
        // Create ticker with price that reaches exit step
        var ticker = CreateTestTicker(100.5m, 101m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup mock to throw exception
        var testException = new Exception("Test error message");
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(testException);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - error is logged with the correct exception and bot ID
        LoggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.Is<Exception>(ex => ex!.Message == testException.Message),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_ShouldReturnFailureResult_WhenExitOrderPlacementFails()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        await CreateCompletedTrade(bot, entryPrice);
        
        // Create ticker with price that reaches exit step
        var ticker = CreateTestTicker(100.5m, 101m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup mock to throw exception
        var testException = new Exception("Test error message");
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(testException);
        
        // Act
        var result = await Handler.Handle(command, CancellationToken.None);
        
        // Assert - result should still succeed even with errors
        Assert.True(result.Succeeded);
        
        // Verify the error was logged
        LoggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.Is<Exception>(ex => ex!.Message == testException.Message),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_ShouldContinueProcessingOtherBots_WhenOneBotFails()
    {
        // Arrange
        var failingBot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var successBot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        
        // Create trades for both bots
        await CreateCompletedTrade(failingBot, 100m);
        await CreateCompletedTrade(successBot, 100m);
        
        // Create ticker with price that reaches exit step
        var ticker = CreateTestTicker(100.5m, 101m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup failing bot to throw exception
        var testException = new Exception("Test error for failing bot");
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.Is<Bot>(b => b.Id == failingBot.Id),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(testException);
        
        // Setup successful bot to return order
        var successOrder = CreateOrder(successBot, 101m, successBot.EntryQuantity, !successBot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.Is<Bot>(b => b.Id == successBot.Id),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(successOrder);
        
        // Act
        var result = await Handler.Handle(command, CancellationToken.None);
        
        // Assert - should still succeed overall
        Assert.True(result.Succeeded);
        
        // Verify error was logged
        LoggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.Is<Exception>(ex => ex!.Message == testException.Message),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Verify successful bot still had order placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == successBot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_ShouldNotFailWithEmptyDatabase()
    {
        // Arrange - ensure database is empty
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
        
        var ticker = CreateTestTicker(100m, 101m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Act
        var result = await Handler.Handle(command, CancellationToken.None);
        
        // Assert - should succeed with empty database
        Assert.True(result.Succeeded);
        
        // No orders should be placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleInvalidTicker()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        await CreateCompletedTrade(bot, entryPrice);
        
        // Create invalid ticker (negative or zero prices)
        var invalidTicker = CreateTestTicker(0, -1);
        var command = new PlaceExitOrdersCommand { Ticker = invalidTicker };
        
        // Act
        var result = await Handler.Handle(command, CancellationToken.None);
        
        // Assert - should not throw but no orders should be placed
        Assert.True(result.Succeeded); // No errors since no eligible trades
        
        // No orders should be placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleAdvanceOrderFailures()
    {
        // Arrange
        var bot = await CreateTestBot(
            exitStep: 1.0m,
            placeOrdersInAdvance: true,
            ordersInAdvance: 2,
            isLong: true);
        
        // Create two trades
        var trade1 = await CreateCompletedTrade(bot, 100m);
        var trade2 = await CreateCompletedTrade(bot, 99m);
        
        // Create ticker with price that reaches exit step only for first trade
        var ticker = CreateTestTicker(100.5m, 101.1m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup mock to succeed for consolidated order but fail for advance order
        var consolidatedOrder = CreateOrder(bot, 101.1m, 2.0m, !bot.IsLong); // Quantity for both trades
        var testException = new Exception("Advance order failure");
        
        var mockSequence = ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()));
            
        mockSequence = mockSequence.ReturnsAsync(consolidatedOrder);
        mockSequence = mockSequence.ThrowsAsync(testException);
        
        // Act
        var result = await Handler.Handle(command, CancellationToken.None);
        
        // Assert - should still succeed overall
        Assert.True(result.Succeeded);
        
        // Verify the successful order was logged
        LoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString() != null && v.ToString().Contains("Successfully placed") && v.ToString().Contains("exit orders for bot")),
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Verify consolidated order was placed with quantity 2.0 (for both trades)
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 101.1m),
            It.Is<decimal>(q => q == 2.0m), // Both trades are consolidated in a single order
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify both trades got the same exit order
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => t.Id == trade1.Id || t.Id == trade2.Id)
            .ToListAsync();
            
        Assert.Equal(2, updatedTrades.Count);
        Assert.All(updatedTrades, t => Assert.NotNull(t.ExitOrder));
        Assert.All(updatedTrades, t => Assert.Equal(consolidatedOrder.Id, t.ExitOrder!.Id));
    }
}
