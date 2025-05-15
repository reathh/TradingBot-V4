using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

/// <summary>
/// Tests for ExitLosingTrades command in various market conditions
/// </summary>
public class ExitLosingTradesMarketTests : ExitLosingTradesTestBase
{
    [Fact]
    public async Task Handle_ShouldExitLongTrades_DuringFlashCrash()
    {
        // Arrange - simulating a flash crash with price dropping 10%
        var entryPrice = 50000m; // BTC price in USD
        var crashedPrice = entryPrice * 0.9m; // 10% flash crash
        var stopLossPercent = 1.0m; // 1% stop loss
        
        // Create bot with different trades at various price points
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Create trades at different entry prices that will all hit stop loss
        var trade1 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        var trade2 = await CreateFilledTrade(bot, entryPrice * 0.98m, 0.01m);
        var trade3 = await CreateFilledTrade(bot, entryPrice * 0.96m, 0.01m);
        var trade4 = await CreateFilledTrade(bot, entryPrice * 0.94m, 0.01m);
        
        var totalQuantity = 0.04m;
        
        // Setup exit order mock
        var exitOrder = CreateOrder(bot, crashedPrice, totalQuantity, false);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        // Create ticker with flash crash price
        var ticker = CreateTicker(crashedPrice, crashedPrice + 10m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify one consolidated exit order was placed with all quantities
        VerifyExitOrderPlaced(
            bot, 
            Times.Once(), 
            expectedPrice: crashedPrice,
            expectedQuantity: totalQuantity,
            expectedIsBuy: false);
            
        // Verify all trades were updated with the exit order
        var savedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        foreach (var trade in savedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(exitOrder.Id, trade.ExitOrder.Id);
        }
    }
    
    [Fact]
    public async Task Handle_ShouldExitShortTrades_DuringShortSqueeze()
    {
        // Arrange - simulating a short squeeze with price rising 10%
        var entryPrice = 50000m; // BTC price in USD
        var squeezedPrice = entryPrice * 1.1m; // 10% short squeeze
        var stopLossPercent = 1.0m; // 1% stop loss
        
        // Create bot with different trades at various price points
        var bot = await CreateStopLossBot(isLong: false, stopLossPercent: stopLossPercent);
        
        // Create trades at different entry prices that will all hit stop loss
        var trade1 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        var trade2 = await CreateFilledTrade(bot, entryPrice * 1.02m, 0.01m);
        var trade3 = await CreateFilledTrade(bot, entryPrice * 1.04m, 0.01m);
        var trade4 = await CreateFilledTrade(bot, entryPrice * 1.06m, 0.01m);
        
        var totalQuantity = 0.04m;
        
        // Setup exit order mock
        var exitOrder = CreateOrder(bot, squeezedPrice, totalQuantity, true);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        // Create ticker with short squeeze price
        var ticker = CreateTicker(squeezedPrice - 10m, squeezedPrice);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify one consolidated exit order was placed with all quantities
        VerifyExitOrderPlaced(
            bot, 
            Times.Once(), 
            expectedPrice: squeezedPrice,
            expectedQuantity: totalQuantity,
            expectedIsBuy: true);
            
        // Verify all trades were updated with the exit order
        var savedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        foreach (var trade in savedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(exitOrder.Id, trade.ExitOrder.Id);
        }
    }
    
    [Fact]
    public async Task Handle_ShouldHandleLargeStopLoss_DuringExtremePriceMovement()
    {
        // Arrange - Testing large stop loss percentage
        var entryPrice = 50000m;
        var crashedPrice = entryPrice * 0.8m; // 20% drop
        var stopLossPercent = 15.0m; // 15% stop loss - large but not triggered by 20% drop
        
        // Create bot with large stop loss
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, 0.01m);
        
        // Setup exit order mock
        var exitOrder = CreateOrder(bot, crashedPrice, 0.01m, false);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        // Create ticker with crash price
        var ticker = CreateTicker(crashedPrice, crashedPrice + 10m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order was placed because price moved more than stop loss
        VerifyExitOrderPlaced(bot, Times.Once());
            
        // Verify trade was updated with exit order
        var savedTrade = await DbContext.Trades.Include(t => t.ExitOrder).FirstAsync();
        Assert.NotNull(savedTrade.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleWideBidAskSpread_DuringLowLiquidity()
    {
        // Arrange - simulating low liquidity conditions with wide spread
        var entryPrice = 50000m; 
        var bidPrice = 49000m; // 2% below entry price
        var askPrice = 51000m; // 2% above entry price
        var stopLossPercent = 1.0m; // 1% stop loss
        
        // Create one long bot and one short bot with same stop loss
        var longBot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var shortBot = await CreateStopLossBot(isLong: false, stopLossPercent: stopLossPercent);
        
        // Add trades to both bots
        var longTrade = await CreateFilledTrade(longBot, entryPrice, 0.01m);
        var shortTrade = await CreateFilledTrade(shortBot, entryPrice, 0.01m);
        
        // Setup exit orders for both bots
        var longExitOrder = CreateOrder(longBot, bidPrice, 0.01m, false);
        var shortExitOrder = CreateOrder(shortBot, askPrice, 0.01m, true);
        
        ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(longExitOrder)
            .ReturnsAsync(shortExitOrder);
        
        // Create ticker with wide spread
        var ticker = CreateTicker(bidPrice, askPrice);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify both bots placed exit orders at the appropriate side of the spread
        VerifyExitOrderPlaced(longBot, Times.Once(), expectedPrice: bidPrice); // Long exits at bid
        VerifyExitOrderPlaced(shortBot, Times.Once(), expectedPrice: askPrice); // Short exits at ask
            
        // Verify both trades were updated with exit orders
        var longBotTrades = await DbContext.Trades.Where(t => t.BotId == longBot.Id).Include(t => t.ExitOrder).ToListAsync();
        var shortBotTrades = await DbContext.Trades.Where(t => t.BotId == shortBot.Id).Include(t => t.ExitOrder).ToListAsync();
        
        Assert.NotNull(longBotTrades.Single().ExitOrder);
        Assert.NotNull(shortBotTrades.Single().ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleHighVolatility_WithMultipleTickerUpdates()
    {
        // Arrange - Testing multiple ticker updates during high volatility
        var entryPrice = 50000m;
        var stopLossPercent = 1.0m; // 1% stop loss
        var stopLossPrice = entryPrice * (1 - stopLossPercent / 100m); // 49,500
        
        // Create bot with trade
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, 0.01m);
        
        // Setup exit order mock
        var exitOrder = CreateOrder(bot, stopLossPrice - 10m, 0.01m, false);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        // First ticker update - price just above stop loss
        var ticker1 = CreateTicker(stopLossPrice + 10m, stopLossPrice + 20m);
        var command1 = new ExitLosingTradesCommand { Ticker = ticker1 };
        
        // Second ticker update - price dropped below stop loss
        var ticker2 = CreateTicker(stopLossPrice - 10m, stopLossPrice);
        var command2 = new ExitLosingTradesCommand { Ticker = ticker2 };

        // Act - Process first ticker update (no stop loss)
        var result1 = await Handle(command1, CancellationToken.None);
        
        // Verify no exit order was placed yet
        VerifyExitOrderPlaced(bot, Times.Never());
        
        // Process second ticker update (should trigger stop loss)
        var result2 = await Handle(command2, CancellationToken.None);

        // Assert
        Assert.True(result1.Succeeded);
        Assert.True(result2.Succeeded);
        
        // Verify exit order was placed with second ticker
        VerifyExitOrderPlaced(bot, Times.Once());
            
        // Verify trade was updated with exit order
        var savedTrade = await DbContext.Trades.Include(t => t.ExitOrder).FirstAsync();
        Assert.NotNull(savedTrade.ExitOrder);
    }
} 