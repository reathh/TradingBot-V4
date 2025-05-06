using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceOpeningOrders;
using TradingBot.Data;

namespace TradingBot.Tests;

public class PlaceOpeningOrdersCommandTests : BaseTest
{
    [Fact]
    public async Task Handle_ShouldPlaceOrder_WhenBotIsEnabledAndNoOpenTrades()
    {
        // Arrange
        var bot = await CreateBot();
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
        var bot = await CreateBot();
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
        var bot = await CreateBot(maxPrice: 100);
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
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceIsBelowMinPrice()
    {
        // Arrange
        var bot = await CreateBot(minPrice: 100);
        var ticker = CreateTicker(98, 99);
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
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceIsBelowMinPrice_ShortBot()
    {
        // Arrange
        var bot = await CreateBot(minPrice: 100, isLong: false);
        var ticker = CreateTicker(98, 99);  // Both bid and ask are below MinPrice
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
    public async Task Handle_ShouldPlaceOrdersInAdvance_WhenConfigured_LongBot()
    {
        // Arrange
        var bot = await CreateBot(
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
    public async Task Handle_ShouldPlaceOrdersInAdvance_WhenConfigured_ShortBot()
    {
        // Arrange
        var bot = await CreateBot(
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            entryStep: 1m,
            isLong: false);
        var ticker = CreateTicker(100, 101);
        var command = new PlaceOpeningOrdersCommand { Ticker = ticker };

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

        // Now update the bot to have 5 orders in advance
        ExchangeApiMock.Reset();
        bot.OrdersInAdvance = 5;
        await DbContext.SaveChangesAsync();

        // Create a new ticker with higher prices (105/106)
        var newTicker = CreateTicker(105, 106);
        var newCommand = new PlaceOpeningOrdersCommand { Ticker = newTicker };

        // We should place 2 orders:
        // 1. For 3 units (for prices 104, 105, and 106) because we need to catch up with price movement
        // 2. For 1 unit (at price 107) as the new order in advance
        var additionalOrders = new[]
        {
            CreateOrder(bot, 106, 3, bot.IsLong),  // Catch up order for 104, 105, 106
            CreateOrder(bot, 107, 1, bot.IsLong)   // Order in advance based on entryQuantity
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
            It.Is<decimal>(p => p == 106),  // First order at price 106
            It.Is<decimal>(q => q == 3),    // For 3 units
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 107),  // Second order at price 107
            It.Is<decimal>(q => q == 1),    // For 1 unit (entryQuantity)
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
        var bot = await CreateBot(entryQuantity: 1, entryStep: 1m, isLong: false);

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
        var bot = await CreateBot(entryQuantity: 1, entryStep: 1m, isLong: true);

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
        var bot = await CreateBot();
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

    [Fact]
    public async Task Handle_ShouldPlaceOrdersForMultipleBots_ThatMeetCriteria()
    {
        // Arrange
        var bot1 = await CreateBot(entryQuantity: 1);
        var bot2 = await CreateBot(entryQuantity: 2);
        var disabledBot = await CreateBot();
        disabledBot.Enabled = false;
        await DbContext.SaveChangesAsync();

        var ticker = CreateTicker(100, 101);
        var command = new PlaceOpeningOrdersCommand { Ticker = ticker };

        var order1 = CreateOrder(bot1, bot1.IsLong ? ticker.Bid : ticker.Ask, bot1.EntryQuantity, bot1.IsLong);
        var order2 = CreateOrder(bot2, bot2.IsLong ? ticker.Bid : ticker.Ask, bot2.EntryQuantity, bot2.IsLong);

        ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order1)
            .ReturnsAsync(order2);

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot1.Id || b.Id == bot2.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        var savedTrades = await DbContext.Trades.ToListAsync();
        Assert.Equal(2, savedTrades.Count);
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

        var command = new PlaceOpeningOrdersCommand { Ticker = ticker };
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

    [Fact]
    public async Task Handle_ShouldPlaceOrder_WhenPriceIsExactlyAtMinPrice()
    {
        // Arrange
        var minPrice = 100m;
        var bot = await CreateBot(minPrice: minPrice);
        var ticker = CreateTicker(minPrice - 1, minPrice); // Ask is exactly at MinPrice
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

        // Assert - Order should be placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
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

        // Assert - Order should be placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
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
    public async Task Handle_ShouldNotPlaceOrders_WhenNoEnabledBotsExist()
    {
        // Arrange
        // Clear any existing bots
        var existingBots = await DbContext.Bots.ToListAsync();
        DbContext.Bots.RemoveRange(existingBots);
        await DbContext.SaveChangesAsync();

        // Create a disabled bot
        var bot = await CreateBot();
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
    public async Task Handle_ShouldPlaceCorrectOrders_WhenEntryStepAndQuantityAreChanged()
    {
        // Arrange
        var bot = await CreateBot(entryQuantity: 1, entryStep: 1m, isLong: true);

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

        // Second ticker: 97/98 (price dropped)
        var secondTicker = CreateTicker(97, 98);
        var secondCommand = new PlaceOpeningOrdersCommand { Ticker = secondTicker };

        // We need orders at 99,98,97 (3 units)
        // We already have an order at 100
        var secondOrder = CreateOrder(bot, 97, 3, bot.IsLong);
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
            It.Is<decimal>(p => p == 97),
            It.Is<decimal>(q => q == 3),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        // Now change the bot's entryStep and entryQuantity
        bot.EntryStep = 2m;     // Larger step - should place fewer orders
        bot.EntryQuantity = 3m; // Larger quantity - each order is larger
        await DbContext.SaveChangesAsync();

        // Clear mock for next test
        ExchangeApiMock.Reset();

        // Third ticker: 93/94 (price dropped further)
        var thirdTicker = CreateTicker(93, 94);
        var thirdCommand = new PlaceOpeningOrdersCommand { Ticker = thirdTicker };

        // With new entry step of 2, we need orders at:
        // 99, 97, 95, 93 (with first at 100, that's 4 price levels from 100 to 93)
        // We already have orders at 100, 99, 98, 97
        // Calculation: (100-93)/2 + 1 = 4.5 -> 4 price levels
        // 4 price levels × 3 units = 12 total units needed
        // We already have 4 units (1 at 100 + 3 at 97-99), so need 8 more
        var thirdOrder = CreateOrder(bot, 93, 8, bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(thirdOrder);

        // Act - Place third order with updated bot parameters
        await Handler.Handle(thirdCommand, CancellationToken.None);

        // Assert - Verify third order with updated parameters
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 93),
            It.Is<decimal>(q => q == 8), // Correct quantity based on calculations
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        // Clear mock for next test
        ExchangeApiMock.Reset();

        // Fourth ticker shows price movement in the opposite direction
        // This tests that the bot doesn't place redundant orders
        var fourthTicker = CreateTicker(95, 96);
        var fourthCommand = new PlaceOpeningOrdersCommand { Ticker = fourthTicker };

        // No new orders needed since we already have orders covering this price range
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order)null!);

        // Act - Try to place fourth order
        await Handler.Handle(fourthCommand, CancellationToken.None);

        // Assert - Verify no new orders were placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}