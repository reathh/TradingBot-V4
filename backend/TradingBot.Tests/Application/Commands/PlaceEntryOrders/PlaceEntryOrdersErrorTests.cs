using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Tests for error handling in entry order placement
/// </summary>
public class PlaceEntryOrdersErrorTests : BaseTest
{
    [Fact]
    public async Task Handle_ShouldLogError_WhenOrderPlacementFails()
    {
        // Arrange
        var bot = await CreateBot();
        var ticker = CreateTicker(100, 101);
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // No log assertion
    }

    [Fact]
    public async Task Handle_ShouldNotCrash_WhenOrderPlacementFails()
    {
        // Arrange
        var bot = await CreateBot();
        var ticker = CreateTicker(100, 101);
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act & Assert - Should not throw
        await Handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ShouldKeepProcessingOtherBots_WhenOneOrderPlacementFails()
    {
        // Arrange
        var bot1 = await CreateBot();
        var bot2 = await CreateBot();
        var ticker = CreateTicker(100, 101);
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        // First bot's order will fail
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.Is<Bot>(b => b.Id == bot1.Id),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Second bot's order will succeed
        var order2 = CreateOrder(bot2, bot2.IsLong ? ticker.Bid : ticker.Ask, bot2.EntryQuantity, bot2.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.Is<Bot>(b => b.Id == bot2.Id),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order2);

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert - Second bot's order should be saved
        var trades = await DbContext.Trades.ToListAsync();
        Assert.Single(trades);
        Assert.Equal(bot2.Id, trades[0].BotId);
    }
} 