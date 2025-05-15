using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Models;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

/// <summary>
/// Basic tests for ExitLosingTrades command functionality
/// </summary>
public class ExitLosingTradesBasicTests : ExitLosingTradesTestBase
{
    [Fact]
    public async Task Handle_ShouldExitLossTrade_ForLongBot_WhenPriceFallsBelowThreshold()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m; // 1% stop loss
        var stopLossPrice = entryPrice * (1 - stopLossPercent / 100m); // 99.0
        var currentBid = stopLossPrice - 0.1m; // 98.9 - just below stop loss
        var currentAsk = currentBid + 0.2m; // 99.1 - ask price for the ticker

        // Create bot and trade
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, bot.EntryQuantity);
        
        // For long positions exiting, we should use ask price for limit maker orders
        var exitPrice = currentAsk;
        
        // Setup exit order mock
        var exitOrder = CreateOrder(bot, exitPrice, bot.EntryQuantity, false);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        // Create ticker with price below stop loss
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order placed at the ask price (for a long position using limit maker)
        VerifyExitOrderPlaced(
            bot, 
            Times.Once(), 
            expectedPrice: exitPrice,
            expectedQuantity: bot.EntryQuantity,
            expectedIsBuy: false);
            
        // Verify trade was updated with exit order
        var savedTrade = await DbContext.Trades.Include(t => t.ExitOrder).FirstAsync();
        Assert.NotNull(savedTrade.ExitOrder);
        Assert.Equal(exitOrder.Id, savedTrade.ExitOrder.Id);
    }
    
    [Fact]
    public async Task Handle_ShouldExitLossTrade_ForShortBot_WhenPriceRisesAboveThreshold()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m; // 1% stop loss
        var stopLossPrice = entryPrice * (1 + stopLossPercent / 100m); // 101.0
        var currentAsk = stopLossPrice + 0.1m; // 101.1 - just above stop loss
        var currentBid = currentAsk - 0.2m; // 100.9 - bid price for the ticker

        // Create bot and trade
        var bot = await CreateStopLossBot(isLong: false, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, bot.EntryQuantity);
        
        // For short positions exiting, we should use bid price for limit maker orders
        var exitPrice = currentBid;
        
        // Setup exit order mock
        var exitOrder = CreateOrder(bot, exitPrice, bot.EntryQuantity, true);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        // Create ticker with price above stop loss
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order placed at the bid price (for a short position using limit maker)
        VerifyExitOrderPlaced(
            bot, 
            Times.Once(), 
            expectedPrice: exitPrice,
            expectedQuantity: bot.EntryQuantity,
            expectedIsBuy: true);
            
        // Verify trade was updated with exit order
        var savedTrade = await DbContext.Trades.Include(t => t.ExitOrder).FirstAsync();
        Assert.NotNull(savedTrade.ExitOrder);
        Assert.Equal(exitOrder.Id, savedTrade.ExitOrder.Id);
    }
    
    [Fact]
    public async Task Handle_ShouldNotExitTrade_WhenPriceIsWithinStopLossTolerance()
    {
        // Arrange - Long bot with 1% stop loss
        var entryPrice = 100m;
        var stopLossPercent = 1.0m;
        var stopLossPrice = entryPrice * (1 - stopLossPercent / 100m); // 99.0
        var currentBid = stopLossPrice + 0.1m; // 99.1 - just above stop loss level
        
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, bot.EntryQuantity);
        
        // Create ticker with price above the stop loss threshold
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify no exit order was placed
        VerifyExitOrderPlaced(bot, Times.Never());
        
        // Verify trade wasn't updated
        var savedTrade = await DbContext.Trades.FirstAsync();
        Assert.Null(savedTrade.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldNotExitTrade_WhenBotStopLossIsDisabled()
    {
        // Arrange
        var entryPrice = 100m;
        var currentBid = 90m; // 10% below entry price
        
        // Create bot with stop loss disabled but still has a stop loss percentage
        var bot = await CreateStopLossBot(stopLossPercent: 1.0m);
        bot.StopLossEnabled = false;
        await DbContext.SaveChangesAsync();
        
        var trade = await CreateFilledTrade(bot, entryPrice, bot.EntryQuantity);
        
        // Create ticker with price way below the entry price
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify no exit order was placed
        VerifyExitOrderPlaced(bot, Times.Never());
        
        // Verify trade wasn't updated
        var savedTrade = await DbContext.Trades.FirstAsync();
        Assert.Null(savedTrade.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldNotExitTrade_WhenBotIsDisabled()
    {
        // Arrange
        var entryPrice = 100m;
        var currentBid = 90m; // 10% below entry price
        
        // Create bot and disable it
        var bot = await CreateStopLossBot(stopLossPercent: 1.0m);
        bot.Enabled = false;
        await DbContext.SaveChangesAsync();
        
        var trade = await CreateFilledTrade(bot, entryPrice, bot.EntryQuantity);
        
        // Create ticker with price way below the entry price
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify no exit order was placed
        VerifyExitOrderPlaced(bot, Times.Never());
        
        // Verify trade wasn't updated
        var savedTrade = await DbContext.Trades.FirstAsync();
        Assert.Null(savedTrade.ExitOrder);
    }
} 