using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Tests for advance entry order placement functionality
/// </summary>
public class PlaceEntryOrdersAdvanceTests : BaseTest
{
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

        ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders[0])
            .ReturnsAsync(orders[1])
            .ReturnsAsync(orders[2]);

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));

        var savedTrades = await DbContext.Trades.ToListAsync();
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

        ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders[0])
            .ReturnsAsync(orders[1])
            .ReturnsAsync(orders[2]);

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));

        var savedTrades = await DbContext.Trades.ToListAsync();
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
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstOrder);

        var command = new PlaceEntryOrdersCommand { Ticker = ticker };
        await Handler.Handle(command, CancellationToken.None);

        // Reset mock for second attempt
        ExchangeApiMock.Reset();

        // Act - Try to place another order
        await Handler.Handle(command, CancellationToken.None);

        // Assert - Verify no additional orders were placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
} 