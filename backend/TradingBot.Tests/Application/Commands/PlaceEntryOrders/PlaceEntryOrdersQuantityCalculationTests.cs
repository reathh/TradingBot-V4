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
public class PlaceEntryOrdersQuantityCalculationTests : BaseTest
{
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
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstOrder);

        // Act - Place first order
        await Handler.Handle(firstCommand, CancellationToken.None);

        // Assert - Verify first order
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100),
            It.Is<decimal>(q => q == 1),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        // Clear mock for next test
        ExchangeApiMock.Reset();

        // Second ticker: 95/96 (price dropped)
        var secondTicker = CreateTicker(95, 96);
        var secondCommand = new PlaceEntryOrdersCommand { Ticker = secondTicker };

        // We need orders at 99,98,97,96,95 (5 units)
        // We already have an order at 100
        var secondOrder = CreateOrder(bot, 95, 5, bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondOrder);

        // Act - Place second order
        await Handler.Handle(secondCommand, CancellationToken.None);

        // Assert - Verify second order
        ExchangeApiMock.Verify(x => x.PlaceOrder(
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
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstOrder);

        // Act - Place first order
        await Handler.Handle(firstCommand, CancellationToken.None);

        // Assert - Verify first order
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 101),
            It.Is<decimal>(q => q == 1),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        // Clear mock for next test
        ExchangeApiMock.Reset();

        // Second ticker: 105/106 (price rose)
        var secondTicker = CreateTicker(105, 106);
        var secondCommand = new PlaceEntryOrdersCommand { Ticker = secondTicker };

        // We need orders at 102,103,104,105,106 (5 units)
        // We already have an order at 101
        var secondOrder = CreateOrder(bot, 106, 5, bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondOrder);

        // Act - Place second order
        await Handler.Handle(secondCommand, CancellationToken.None);

        // Assert - Verify second order
        ExchangeApiMock.Verify(x => x.PlaceOrder(
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
        await DbContext.SaveChangesAsync();

        // Create a ticker with price very close to existing order to generate 0 quantity
        // The price movement is less than entryStep, so no new orders should be placed
        var ticker = CreateTicker(99.9m, 100.2m);
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
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
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstOrder);

        // Act - Place first order
        await Handler.Handle(firstCommand, CancellationToken.None);

        // Now change the bot's entryStep and entryQuantity
        bot.EntryStep = 0.5m;  // Smaller step
        bot.EntryQuantity = 2; // Larger quantity
        await DbContext.SaveChangesAsync();

        // Clear mock for next test
        ExchangeApiMock.Reset();

        // Second ticker: 94/95 (price dropped)
        var secondTicker = CreateTicker(94, 95);
        var secondCommand = new PlaceEntryOrdersCommand { Ticker = secondTicker };

        // We now have steps of 0.5, so we need orders at:
        // 99.5, 99, 98.5, 98, 97.5, 97, 96.5, 96, 95.5, 95, 94.5, 94 = 12 steps
        // Each order should be quantity 2, so 12 * 2 = 24 total quantity
        var secondOrder = CreateOrder(bot, 94, 24, bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondOrder);

        // Act - Place second order with new parameters
        await Handler.Handle(secondCommand, CancellationToken.None);

        // Assert - Verify second order has correct quantity based on new parameters
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 94),
            It.Is<decimal>(q => q == 24),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
    }
} 