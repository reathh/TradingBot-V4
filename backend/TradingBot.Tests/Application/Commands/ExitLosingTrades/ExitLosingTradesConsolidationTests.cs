using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

/// <summary>
/// Tests for ExitLosingTrades command functionality related to consolidating trades
/// </summary>
public class ExitLosingTradesConsolidationTests : ExitLosingTradesTestBase
{
    [Fact]
    public async Task Handle_ShouldConsolidateMultipleTrades_IntoSingleExitOrder()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m; // 1% stop loss
        var stopLossPrice = entryPrice * (1 - stopLossPercent / 100m); // 99.0
        var currentBid = stopLossPrice - 0.1m; // 98.9 - just below stop loss
        var currentAsk = currentBid + 0.2m; // 99.1 - ask price
        
        // Create a bot with 3 trades at the same price point
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        var trade1 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        var trade2 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        var trade3 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        
        var totalQuantity = 0.03m; // Sum of all trade quantities
        
        // Setup exit order mock - long bot exits at ask price
        var exitOrder = CreateOrder(bot, currentAsk, totalQuantity, false);
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
        
        // Verify only one exit order was placed with the combined quantity
        VerifyExitOrderPlaced(
            bot, 
            Times.Once(), 
            expectedPrice: currentAsk,
            expectedQuantity: totalQuantity,
            expectedIsBuy: false);
            
        // Verify all trades were updated with the same exit order
        var savedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        Assert.Equal(3, savedTrades.Count);
        
        foreach (var trade in savedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(exitOrder.Id, trade.ExitOrder.Id);
        }
    }
    
    [Fact]
    public async Task Handle_ShouldConsolidateTradesWithDifferentEntryPrices_IntoSingleExitOrder()
    {
        // Arrange
        var stopLossPercent = 2.0m; // 2% stop loss
        var currentBid = 95m; // Price that will trigger stop loss for all trades
        
        // Create a bot with 3 trades at different price points
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // All these trades will hit stop loss at different price points
        var trade1 = await CreateFilledTrade(bot, 100m, 0.01m); // Stop loss at 98.0
        var trade2 = await CreateFilledTrade(bot, 98m, 0.01m);  // Stop loss at 96.04
        var trade3 = await CreateFilledTrade(bot, 97m, 0.01m);  // Stop loss at 95.06
        
        var totalQuantity = 0.03m; // Sum of all trade quantities
        
        // Setup exit order mock
        var exitOrder = CreateOrder(bot, currentBid, totalQuantity, false);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        // Create ticker with price below stop loss for all trades
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify only one exit order was placed with the combined quantity
        VerifyExitOrderPlaced(
            bot, 
            Times.Once(), 
            expectedPrice: currentBid,
            expectedQuantity: totalQuantity,
            expectedIsBuy: false);
            
        // Verify all trades were updated with the same exit order
        var savedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        Assert.Equal(3, savedTrades.Count);
        
        foreach (var trade in savedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(exitOrder.Id, trade.ExitOrder.Id);
        }
    }
    
    [Fact]
    public async Task Handle_ShouldConsolidateOnlyEligibleTrades_ForStopLossExit()
    {
        // Arrange
        var stopLossPercent = 1.0m; // 1% stop loss
        var currentBid = 98.5m; // Price that will trigger stop loss for trade1 only
        
        // Create a bot with 3 trades at different price points
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Only trade1 will hit stop loss
        var trade1 = await CreateFilledTrade(bot, 100m, 0.01m); // Stop loss at 99.0
        var trade2 = await CreateFilledTrade(bot, 97m, 0.01m);  // Stop loss at 96.03
        var trade3 = await CreateFilledTrade(bot, 96m, 0.01m);  // Stop loss at 95.04
        
        // Setup exit order mock for only the eligible trade quantity
        var exitOrder = CreateOrder(bot, currentBid, 0.01m, false);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        // Create ticker with price below stop loss for trade1 only
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order was placed with only trade1's quantity
        VerifyExitOrderPlaced(
            bot, 
            Times.Once(), 
            expectedPrice: currentBid,
            expectedQuantity: 0.01m,
            expectedIsBuy: false);
            
        // Verify only trade1 has an exit order
        var savedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        Assert.Equal(3, savedTrades.Count);
        
        var tradesWithExitOrder = savedTrades.Count(t => t.ExitOrder != null);
        Assert.Equal(1, tradesWithExitOrder);
        
        var tradeWithExit = savedTrades.Single(t => t.ExitOrder != null);
        Assert.Equal(100m, tradeWithExit.EntryOrder.Price); // Should be trade1
    }
    
    [Fact]
    public async Task Handle_ShouldProcessMultipleBots_Independently()
    {
        // Arrange
        var stopLossPercent = 1.0m; // 1% stop loss
        var currentBid = 98.5m; // Price that will trigger stop loss
        
        // Create two bots with the same stop loss settings
        var bot1 = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var bot2 = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Add trades to both bots that will hit stop loss
        var bot1Trade = await CreateFilledTrade(bot1, 100m, 0.01m); // Stop loss at 99.0
        var bot2Trade = await CreateFilledTrade(bot2, 100m, 0.02m); // Stop loss at 99.0
        
        // Setup exit order mocks with different return values for verification
        var exitOrder1 = CreateOrder(bot1, currentBid, 0.01m, false);
        var exitOrder2 = CreateOrder(bot2, currentBid, 0.02m, false);
        
        ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder1)
            .ReturnsAsync(exitOrder2);
        
        // Create ticker with price below stop loss for both bots
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit orders were placed for both bots
        VerifyExitOrderPlaced(bot1, Times.Once());
        VerifyExitOrderPlaced(bot2, Times.Once());
            
        // Verify both bot trades were updated with exit orders
        var bot1Trades = await DbContext.Trades.Where(t => t.BotId == bot1.Id).Include(t => t.ExitOrder).ToListAsync();
        var bot2Trades = await DbContext.Trades.Where(t => t.BotId == bot2.Id).Include(t => t.ExitOrder).ToListAsync();
        
        Assert.NotNull(bot1Trades.Single().ExitOrder);
        Assert.NotNull(bot2Trades.Single().ExitOrder);
        
        // Verify different exit orders were used
        Assert.NotEqual(bot1Trades.Single().ExitOrder.Id, bot2Trades.Single().ExitOrder.Id);
    }
} 