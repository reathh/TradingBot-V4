using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceOpeningOrders;
using TradingBot.Data;
using TradingBot.Services;
using Xunit;

namespace TradingBot.Tests;

public class PlaceOpeningOrdersCommandTests : BaseTest
{
    [Fact]
    public async Task Handle_ShouldPlaceOrder_WhenBotIsEnabledAndNoOpenTrades()
    {
        // Arrange
        var bot = await CreateBotAsync();
        var ticker = CreateTicker(100, 101);
        var command = new PlaceOpeningOrdersCommand { Ticker = ticker };

        var expectedOrder = CreateOrder(bot, bot.IsLong ? ticker.Bid : ticker.Ask, bot.EntryQuantity, bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == (bot.IsLong ? ticker.Bid : ticker.Ask)),
            It.Is<decimal>(q => q == bot.EntryQuantity),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        var savedTrade = await DbContext.Trades.FirstOrDefaultAsync();
        Assert.NotNull(savedTrade);
        Assert.Equal(expectedOrder.Price, savedTrade.EntryOrder.Price);
        Assert.Equal(expectedOrder.Quantity, savedTrade.EntryOrder.Quantity);
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenBotIsDisabled()
    {
        // Arrange
        var bot = await CreateBotAsync();
        bot.Enabled = false;
        await DbContext.SaveChangesAsync();

        var ticker = CreateTicker(100, 101);
        var command = new PlaceOpeningOrdersCommand { Ticker = ticker };

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
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceIsAboveMaxPrice()
    {
        // Arrange
        var bot = await CreateBotAsync(maxPrice: 100);
        var ticker = CreateTicker(101, 102);
        var command = new PlaceOpeningOrdersCommand { Ticker = ticker };

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
    public async Task Handle_ShouldPlaceOrdersInAdvance_WhenConfigured()
    {
        // Arrange
        var bot = await CreateBotAsync(
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            entryStep: 1m);
        var ticker = CreateTicker(100, 101);
        var command = new PlaceOpeningOrdersCommand { Ticker = ticker };

        var orders = new[]
        {
            CreateOrder(bot, bot.IsLong ? ticker.Bid : ticker.Ask, bot.EntryQuantity, bot.IsLong),
            CreateOrder(bot, bot.IsLong ? ticker.Bid + 1 : ticker.Ask - 1, bot.EntryQuantity, bot.IsLong),
            CreateOrder(bot, bot.IsLong ? ticker.Bid + 2 : ticker.Ask - 2, bot.EntryQuantity, bot.IsLong)
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

        // Now update the bot to have 5 orders in advance
        ExchangeApiMock.Reset();
        bot.OrdersInAdvance = 5;
        await DbContext.SaveChangesAsync();

        // Create a new ticker with lower prices (96/100)
        var newTicker = CreateTicker(96, 100);
        var newCommand = new PlaceOpeningOrdersCommand { Ticker = newTicker };

        // We should place 2 orders:
        // 1. For 2 units (for prices 97 and 96) because we need to catch up with price movement
        // 2. For 1 unit (at price 95) as the new order in advance
        var additionalOrders = new[]
        {
            CreateOrder(bot, 96, 2, bot.IsLong),  // Catch up order for 97 and 96
            CreateOrder(bot, 95, 1, bot.IsLong)   // Order in advance based on entryQuantity
        };

        ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(additionalOrders[0])
            .ReturnsAsync(additionalOrders[1]);

        // Act again with the new ticker command
        await Handler.Handle(newCommand, CancellationToken.None);

        // Assert that both orders were placed with correct parameters
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 96),  // First order at price 96
            It.Is<decimal>(q => q == 2),   // For 2 units
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 95),  // Second order at price 95
            It.Is<decimal>(q => q == 1),   // For 1 unit (entryQuantity)
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        // Check that we now have 5 trades total
        savedTrades = await DbContext.Trades.ToListAsync();
        Assert.Equal(5, savedTrades.Count);
    }

    [Fact]
    public async Task Handle_ShouldCalculateCorrectQuantity_WhenPriceMovesUpAndDown_ShortBot()
    {
        // Arrange
        var bot = await CreateBotAsync(entryQuantity: 1, entryStep: 1m, isLong: false);

        // First ticker: 100/101
        var firstTicker = CreateTicker(100, 101);
        var firstCommand = new PlaceOpeningOrdersCommand { Ticker = firstTicker };

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
        var secondCommand = new PlaceOpeningOrdersCommand { Ticker = secondTicker };

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

        // Clear mock for next test
        ExchangeApiMock.Reset();

        // Third ticker: 103/104 (price fell)
        var thirdTicker = CreateTicker(103, 104);
        var thirdCommand = new PlaceOpeningOrdersCommand { Ticker = thirdTicker };

        // Price fell to 104, we already have orders at 101,102,103,104,105,106
        // No new orders needed
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order)null!);

        // Act - Place third order
        await Handler.Handle(thirdCommand, CancellationToken.None);

        // Assert - Verify no new order was placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);

        // Clear mock for next test
        ExchangeApiMock.Reset();

        // Fourth ticker: 110/111 (price rose further)
        var fourthTicker = CreateTicker(110, 111);
        var fourthCommand = new PlaceOpeningOrdersCommand { Ticker = fourthTicker };

        // We need orders at 107,108,109,110,111 (5 units)
        // We already have orders at 101,102,103,104,105,106
        // Total needed: (111-101)/1 + 1 = 11 orders, we have 6, so 5 more needed
        var fourthOrder = CreateOrder(bot, 111, 5, bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fourthOrder);

        // Act - Place fourth order
        await Handler.Handle(fourthCommand, CancellationToken.None);

        // Assert - Verify fourth order
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 111),
            It.Is<decimal>(q => q == 5),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCalculateCorrectQuantity_WhenPriceMovesUpAndDown_LongBot()
    {
        // Arrange
        var bot = await CreateBotAsync(entryQuantity: 1, entryStep: 1m, isLong: true);

        // First ticker: 100/101
        var firstTicker = CreateTicker(100, 101);
        var firstCommand = new PlaceOpeningOrdersCommand { Ticker = firstTicker };

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
        var secondCommand = new PlaceOpeningOrdersCommand { Ticker = secondTicker };

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

        // Clear mock for next test
        ExchangeApiMock.Reset();

        // Third ticker: 97/98 (price rose)
        var thirdTicker = CreateTicker(97, 98);
        var thirdCommand = new PlaceOpeningOrdersCommand { Ticker = thirdTicker };

        // Price rose to 97, we already have orders at 100,99,98,97,96,95
        // No new orders needed
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order)null!);

        // Act - Place third order
        await Handler.Handle(thirdCommand, CancellationToken.None);

        // Assert - Verify no new order was placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);

        // Clear mock for next test
        ExchangeApiMock.Reset();

        // Fourth ticker: 91/92 (price dropped further)
        var fourthTicker = CreateTicker(91, 92);
        var fourthCommand = new PlaceOpeningOrdersCommand { Ticker = fourthTicker };

        // We need orders at 94,93,92,91 (4 units)
        // We already have orders at 100,99,98,97,96,95
        // Total needed: (100-91)/1 + 1 = 10 orders, we have 6, so 4 more needed
        var fourthOrder = CreateOrder(bot, 91, 4, bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fourthOrder);

        // Act - Place fourth order
        await Handler.Handle(fourthCommand, CancellationToken.None);

        // Assert - Verify fourth order
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 91),
            It.Is<decimal>(q => q == 4),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogError_WhenOrderPlacementFails()
    {
        // Arrange
        var bot = await CreateBotAsync();
        var ticker = CreateTicker(100, 101);
        var command = new PlaceOpeningOrdersCommand { Ticker = ticker };

        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert
        LoggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}