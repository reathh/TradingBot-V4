using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Data;
using TradingBot.Services;
using Xunit;

namespace TradingBot.Tests;

public class PlaceExitOrdersCommandTests : BaseTest
{
    [Fact]
    public async Task Handle_ShouldPlaceExitOrder_WhenPriceReachesExitStep()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m);
        var entryPrice = bot.IsLong ? 100m : 101m;
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        entryOrder.Closed = true;
        entryOrder.QuantityFilled = entryOrder.Quantity;
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();

        // Create ticker with price that reaches exit step
        var exitPrice = bot.IsLong ? 101m : 100m; // Up for long, down for short
        var ticker = bot.IsLong
            ? CreateTicker(100.5m, 101m) // Bid is 100.5, Ask is 101
            : CreateTicker(100m, 100.5m); // Bid is 100, Ask is 100.5

        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Setup mock for exit order
        var expectedExitOrder = CreateOrder(bot, exitPrice, bot.EntryQuantity, !bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedExitOrder);

        // Act
        await Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == (bot.IsLong ? ticker.Ask : ticker.Bid)),
            It.Is<decimal>(q => q == bot.EntryQuantity),
            It.Is<bool>(b => b != bot.IsLong), // Exit is opposite of entry
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify trade was updated
        var updatedTrade = await DbContext.Trades.FirstOrDefaultAsync();
        Assert.NotNull(updatedTrade);
        Assert.NotNull(updatedTrade.ExitOrder);
        Assert.Equal(expectedExitOrder.Price, updatedTrade.ExitOrder.Price);
        Assert.Equal(expectedExitOrder.Quantity, updatedTrade.ExitOrder.Quantity);
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceExitOrder_WhenPriceIsLowerThanExitStep_LongBot()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        entryOrder.Closed = true;
        entryOrder.QuantityFilled = entryOrder.Quantity;
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();

        // Create ticker with price that doesn't reach exit step
        var ticker = CreateTicker(100.5m, 100.8m); // Both below exit price of 101
        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Act
        await Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceExitOrder_WhenPriceIsHigherThanExitStep_ShortBot()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m, isLong: false);
        var entryPrice = 101m;
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        entryOrder.Closed = true;
        entryOrder.QuantityFilled = entryOrder.Quantity;
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();

        // Create ticker with price that doesn't reach exit step
        var ticker = CreateTicker(100.3m, 100.5m); // Both above exit price of 100
        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Act
        await Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceExitOrder_WhenBotIsDisabled()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m);
        var entryPrice = bot.IsLong ? 100m : 101m;
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        entryOrder.Closed = true;
        entryOrder.QuantityFilled = entryOrder.Quantity;
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);

        // Disable the bot
        bot.Enabled = false;
        await DbContext.SaveChangesAsync();

        // Create ticker with price that reaches exit step
        var exitPrice = bot.IsLong ? 101m : 100m;
        var ticker = bot.IsLong
            ? CreateTicker(100.5m, 101m)
            : CreateTicker(100m, 100.5m);

        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Act
        await Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPlaceExitOrdersForMultipleTrades_WhenPriceReachesExitStep()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m);

        // Create multiple trades at different entry prices
        var entryPrices = bot.IsLong
            ? new[] { 100m, 99m, 98m }
            : new[] { 101m, 102m, 103m };

        foreach (var price in entryPrices)
        {
            var entryOrder = CreateOrder(bot, price, bot.EntryQuantity, bot.IsLong);
            entryOrder.Closed = true;
            entryOrder.QuantityFilled = entryOrder.Quantity;
            var trade = new Trade(entryOrder);
            bot.Trades.Add(trade);
        }
        await DbContext.SaveChangesAsync();

        // Create ticker with price that reaches exit step for all trades
        var ticker = bot.IsLong
            ? CreateTicker(100.5m, 101m) // Exit at 101 for long (entry + exitStep)
            : CreateTicker(100m, 100.5m); // Exit at 100 for short (entry - exitStep)

        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Setup mocks for exit orders
        var exitOrders = new List<Order>();
        foreach (var price in entryPrices)
        {
            var exitPrice = bot.IsLong ? price + bot.ExitStep : price - bot.ExitStep;
            var exitOrder = CreateOrder(bot, bot.IsLong ? ticker.Ask : ticker.Bid, bot.EntryQuantity, !bot.IsLong);
            exitOrders.Add(exitOrder);
        }

        var sequence = ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()));

        foreach (var order in exitOrders)
        {
            sequence = sequence.ReturnsAsync(order);
        }

        // Act
        await Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(exitOrders.Count));

        // Verify all trades have exit orders
        var updatedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        foreach (var trade in updatedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
        }
    }

    [Fact]
    public async Task Handle_ShouldPlaceExitOrdersInAdvance_WhenConfigured_LongBot()
    {
        // Arrange
        var bot = await CreateBotAsync(
            exitStep: 1.0m,
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            isLong: true);

        // Create multiple trades at different entry prices
        var entryPrices = new[] { 100m, 99m, 98m };
        foreach (var price in entryPrices)
        {
            var entryOrder = CreateOrder(bot, price, bot.EntryQuantity, bot.IsLong);
            entryOrder.Closed = true;
            entryOrder.QuantityFilled = entryOrder.Quantity;
            var trade = new Trade(entryOrder);
            bot.Trades.Add(trade);
        }
        await DbContext.SaveChangesAsync();

        // Create ticker with price that reaches exit step for all trades
        var ticker = CreateTicker(100.5m, 101m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Setup exit orders - one at each exit price
        var exitOrders = new[]
        {
            CreateOrder(bot, 101m, bot.EntryQuantity, !bot.IsLong), // Exit for 100
            CreateOrder(bot, 100m, bot.EntryQuantity, !bot.IsLong), // Exit for 99
            CreateOrder(bot, 99m, bot.EntryQuantity, !bot.IsLong)   // Exit for 98
        };

        var sequence = ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()));

        foreach (var order in exitOrders)
        {
            sequence = sequence.ReturnsAsync(order);
        }

        // Act
        await Handle(command, CancellationToken.None);

        // Assert - exit orders placed for all trades
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Exactly(exitOrders.Length));

        // Verify all trades have exit orders
        var updatedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        foreach (var trade in updatedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
        }
    }

    [Fact]
    public async Task Handle_ShouldPlaceExitOrdersInAdvance_WhenConfigured_ShortBot()
    {
        // Arrange
        var bot = await CreateBotAsync(
            exitStep: 1.0m,
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            isLong: false);

        // Create multiple trades at different entry prices
        var entryPrices = new[] { 101m, 102m, 103m };
        foreach (var price in entryPrices)
        {
            var entryOrder = CreateOrder(bot, price, bot.EntryQuantity, bot.IsLong);
            entryOrder.Closed = true;
            entryOrder.QuantityFilled = entryOrder.Quantity;
            var trade = new Trade(entryOrder);
            bot.Trades.Add(trade);
        }
        await DbContext.SaveChangesAsync();

        // Create ticker with price that reaches exit step for all trades
        var ticker = CreateTicker(100m, 100.5m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Setup exit orders - one at each exit price
        var exitOrders = new[]
        {
            CreateOrder(bot, 100m, bot.EntryQuantity, !bot.IsLong), // Exit for 101
            CreateOrder(bot, 101m, bot.EntryQuantity, !bot.IsLong), // Exit for 102
            CreateOrder(bot, 102m, bot.EntryQuantity, !bot.IsLong)  // Exit for 103
        };

        var sequence = ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()));

        foreach (var order in exitOrders)
        {
            sequence = sequence.ReturnsAsync(order);
        }

        // Act
        await Handle(command, CancellationToken.None);

        // Assert - exit orders placed for all trades
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Exactly(exitOrders.Length));

        // Verify all trades have exit orders
        var updatedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        foreach (var trade in updatedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
        }
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceExitOrder_WhenTradeAlreadyHasExitOrder()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m);
        var entryPrice = bot.IsLong ? 100m : 101m;
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        entryOrder.Closed = true;
        entryOrder.QuantityFilled = entryOrder.Quantity;
        var exitPrice = bot.IsLong ? 101m : 100m;
        var exitOrder = CreateOrder(bot, exitPrice, bot.EntryQuantity, !bot.IsLong);

        var trade = new Trade(entryOrder);
        trade.ExitOrder = exitOrder; // Already has exit order
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();

        // Create ticker with price that reaches exit step
        var ticker = bot.IsLong
            ? CreateTicker(100.5m, 101m)
            : CreateTicker(100m, 100.5m);

        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Act
        await Handle(command, CancellationToken.None);

        // Assert - no orders placed since trade already has exit order
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldLogError_WhenExitOrderPlacementFails()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m);
        var entryPrice = bot.IsLong ? 100m : 101m;
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        entryOrder.Closed = true;
        entryOrder.QuantityFilled = entryOrder.Quantity;
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();

        // Create ticker with price that reaches exit step
        var ticker = bot.IsLong
            ? CreateTicker(100.5m, 101m)
            : CreateTicker(100m, 100.5m);

        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Setup mock to throw exception
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        await Handle(command, CancellationToken.None);

        // Assert - error is logged
        _exitLoggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPlaceExitOrders_ForMultipleBots_ThatMeetCriteria()
    {
        // Arrange
        var bot1 = await CreateBotAsync(exitStep: 1.0m, entryQuantity: 1);
        var bot2 = await CreateBotAsync(exitStep: 1.0m, entryQuantity: 2);
        var disabledBot = await CreateBotAsync(exitStep: 1.0m);
        disabledBot.Enabled = false;

        // Create trades for each bot
        foreach (var bot in new[] { bot1, bot2, disabledBot })
        {
            var entryPrice = bot.IsLong ? 100m : 101m;
            var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
            entryOrder.Closed = true;
            entryOrder.QuantityFilled = entryOrder.Quantity;
            var trade = new Trade(entryOrder);
            bot.Trades.Add(trade);
        }
        await DbContext.SaveChangesAsync();

        // Create ticker with price that reaches exit step
        var ticker = CreateTicker(100.5m, 101m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Setup mock for exit orders
        var exitOrder1 = CreateOrder(bot1, bot1.IsLong ? 101m : 100m, bot1.EntryQuantity, !bot1.IsLong);
        var exitOrder2 = CreateOrder(bot2, bot2.IsLong ? 101m : 100m, bot2.EntryQuantity, !bot2.IsLong);

        ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder1)
            .ReturnsAsync(exitOrder2);

        // Act
        await Handle(command, CancellationToken.None);

        // Assert - orders placed only for enabled bots
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot1.Id || b.Id == bot2.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == disabledBot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateExitOrder_WhenPriceIsExactlyAtExitStep()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m);
        var entryPrice = bot.IsLong ? 100m : 101m;
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        entryOrder.Closed = true;
        entryOrder.QuantityFilled = entryOrder.Quantity;
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();

        // Create ticker with price exactly at exit step
        var ticker = bot.IsLong
            ? CreateTicker(101m, 101m) // Both bid and ask at exit price for long
            : CreateTicker(100m, 100m); // Both bid and ask at exit price for short

        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Setup mock for exit order
        var exitPrice = bot.IsLong ? 101m : 100m;
        var exitOrder = CreateOrder(bot, exitPrice, bot.EntryQuantity, !bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);

        // Act
        await Handle(command, CancellationToken.None);

        // Assert - exit order placed at exact price
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == exitPrice),
            It.Is<decimal>(q => q == bot.EntryQuantity),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCalculateCorrectExitPrices_WhenExitStepIsChanged()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m);
        var entryPrice = bot.IsLong ? 100m : 101m;
        var entryOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        entryOrder.Closed = true;
        entryOrder.QuantityFilled = entryOrder.Quantity;
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();

        // Create ticker with price that doesn't yet reach exit step
        var ticker = bot.IsLong
            ? CreateTicker(100.5m, 100.8m) // Below exit price of 101
            : CreateTicker(100.3m, 100.5m); // Above exit price of 100

        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Act - first attempt with original exit step
        await Handle(command, CancellationToken.None);

        // Assert - no orders placed as price isn't at exit step yet
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);

        // Now reduce exit step to 0.5
        bot.ExitStep = 0.5m;
        await DbContext.SaveChangesAsync();

        // Setup mock for exit order
        var exitPrice = bot.IsLong ? 100.5m : 100.5m;
        var exitOrder = CreateOrder(bot, exitPrice, bot.EntryQuantity, !bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);

        // Act - second attempt with reduced exit step
        await Handle(command, CancellationToken.None);

        // Assert - exit order placed with new exit step
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Add a new test that specifically verifies exit orders are only placed for trades with completed entry orders
    [Fact]
    public async Task Handle_ShouldNotPlaceExitOrder_WhenEntryOrderIsNotCompleted()
    {
        // Arrange
        var bot = await CreateBotAsync(exitStep: 1.0m);
        var entryPrice = bot.IsLong ? 100m : 101m;

        // Create first entry order that is NOT completed (not closed)
        var incompleteOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        incompleteOrder.Closed = false;
        incompleteOrder.QuantityFilled = 0;
        var incompleteTrade = new Trade(incompleteOrder);
        bot.Trades.Add(incompleteTrade);

        // Create second entry order that is completed but has zero quantity filled
        var zeroFilledOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        zeroFilledOrder.Closed = true;
        zeroFilledOrder.QuantityFilled = 0;
        var zeroFilledTrade = new Trade(zeroFilledOrder);
        bot.Trades.Add(zeroFilledTrade);

        // Create third entry order that is both closed and has quantity filled
        var completedOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong);
        completedOrder.Closed = true;
        completedOrder.QuantityFilled = bot.EntryQuantity;
        var completedTrade = new Trade(completedOrder);
        bot.Trades.Add(completedTrade);

        await DbContext.SaveChangesAsync();

        // Create ticker with price that reaches exit step
        var exitPrice = bot.IsLong ? 101m : 100m;
        var ticker = bot.IsLong
            ? CreateTicker(100.5m, 101m)
            : CreateTicker(100m, 100.5m);

        var command = new PlaceExitOrdersCommand { Ticker = ticker };

        // Setup mock for exit order for completed trade
        var exitOrder = CreateOrder(bot, exitPrice, completedOrder.QuantityFilled, !bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);

        // Act
        await Handle(command, CancellationToken.None);

        // Assert - verify exit order was placed only for the completed trade
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Check incomplete trade has no exit order
        var updatedIncompleteTrade = await DbContext.Trades.FirstOrDefaultAsync(t => t.Id == incompleteTrade.Id);
        Assert.NotNull(updatedIncompleteTrade);
        Assert.Null(updatedIncompleteTrade.ExitOrder);

        // Check zero filled trade has no exit order
        var updatedZeroFilledTrade = await DbContext.Trades.FirstOrDefaultAsync(t => t.Id == zeroFilledTrade.Id);
        Assert.NotNull(updatedZeroFilledTrade);
        Assert.Null(updatedZeroFilledTrade.ExitOrder);

        // Check completed trade has exit order
        var updatedCompletedTrade = await DbContext.Trades.FirstOrDefaultAsync(t => t.Id == completedTrade.Id);
        Assert.NotNull(updatedCompletedTrade);
        Assert.NotNull(updatedCompletedTrade.ExitOrder);
    }

    // Replace the Handler field and constructor
    private readonly PlaceExitOrdersCommand.PlaceExitOrdersCommandHandler _exitOrdersHandler;
    private readonly Mock<ILogger<PlaceExitOrdersCommand.PlaceExitOrdersCommandHandler>> _exitLoggerMock;

    public PlaceExitOrdersCommandTests()
    {
        _exitLoggerMock = new Mock<ILogger<PlaceExitOrdersCommand.PlaceExitOrdersCommandHandler>>();
        _exitOrdersHandler = new PlaceExitOrdersCommand.PlaceExitOrdersCommandHandler(
            DbContext,
            ExchangeApiMock.Object,
            _exitLoggerMock.Object);
    }

    // Helper method to handle exit orders commands
    private Task Handle(PlaceExitOrdersCommand command, CancellationToken cancellationToken = default)
    {
        return _exitOrdersHandler.Handle(command, cancellationToken);
    }
}