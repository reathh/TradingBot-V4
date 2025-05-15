using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

/// <summary>
/// Tests for edge cases and error handling in ExitLosingTrades command
/// </summary>
public class ExitLosingTradesEdgeCaseTests : ExitLosingTradesTestBase
{
    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenQuantityBelowMinimum()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m;
        var currentBid = 98.5m; // Below stop loss threshold

        // Create bot and trade
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, 0.01m, fee: 0.005m); // High fee
        
        // Setup symbol info with a minimum quantity higher than available after fees
        var symbolInfo = new SymbolInfo(0.01m, 0.01m, 2);
        SymbolInfoCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(symbolInfo);
        
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify no exit order was placed because quantity is below minimum
        VerifyExitOrderPlaced(bot, Times.Never());
        
        // Verify no trades were updated
        var savedTrade = await DbContext.Trades.FirstAsync();
        Assert.Null(savedTrade.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleRoundingProperly_WithStepSize()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m;
        var currentBid = 98.5m; // Below stop loss threshold
        var currentAsk = currentBid + 0.2m; // 98.7

        // Create bot and trade
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Create trades with quantities that need rounding
        var trade1 = await CreateFilledTrade(bot, entryPrice, 0.01234m, fee: 0.0001m);
        var trade2 = await CreateFilledTrade(bot, entryPrice, 0.05678m, fee: 0.0002m);
        
        // Setup symbol info with step size that requires rounding
        var symbolInfo = new SymbolInfo(0.001m, 0.001m, 3);
        SymbolInfoCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(symbolInfo);
        
        // Expected rounded quantity (0.01234 + 0.05678 - 0.0001 - 0.0002) = 0.06882, rounded to 0.068
        var expectedQuantity = 0.068m;
        
        // Setup exit order mock - long exits at ask price
        var exitOrder = CreateOrder(bot, currentAsk, expectedQuantity, false);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order was placed with properly rounded quantity
        VerifyExitOrderPlaced(
            bot, 
            Times.Once(), 
            expectedPrice: currentAsk,
            expectedQuantity: expectedQuantity,
            expectedIsBuy: false);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleExchangeApiError_WithoutCrashing()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m;
        var currentBid = 98.5m; // Below stop loss threshold

        // Create bot and trade
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, 0.01m);
        
        // Setup API to throw exception
        SetupExitOrderFailure("Exchange API error");
        
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert - command should fail gracefully
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("Bot"));
        
        // Verify no trades were updated
        var savedTrade = await DbContext.Trades.FirstAsync();
        Assert.Null(savedTrade.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldContinueProcessingOtherBots_WhenOneErrors()
    {
        // Arrange
        var stopLossPercent = 1.0m;
        var currentBid = 98.5m; // Below stop loss threshold

        // Create two bots
        var bot1 = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var bot2 = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Add trades to both bots
        var trade1 = await CreateFilledTrade(bot1, 100m, 0.01m);
        var trade2 = await CreateFilledTrade(bot2, 100m, 0.01m);
        
        // Setup API to throw exception for first bot but succeed for second bot
        var mockSequence = ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()));
                
        mockSequence.ThrowsAsync(new Exception("Exchange API error for bot1"));
        
        var exitOrder2 = CreateOrder(bot2, currentBid, 0.01m, false);
        mockSequence.ReturnsAsync(exitOrder2);
        
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert - command should fail due to first bot error
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains(bot1.Id.ToString()));
        
        // Verify bot2's trade was still updated with exit order despite bot1's error
        var bot1Trade = await DbContext.Trades
            .Where(t => t.BotId == bot1.Id)
            .Include(t => t.ExitOrder)
            .FirstAsync();
            
        var bot2Trade = await DbContext.Trades
            .Where(t => t.BotId == bot2.Id)
            .Include(t => t.ExitOrder)
            .FirstAsync();
            
        Assert.Null(bot1Trade.ExitOrder);
        Assert.NotNull(bot2Trade.ExitOrder);
        Assert.Equal(exitOrder2.Id, bot2Trade.ExitOrder.Id);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleTradesWithNoQuantityLeft_AfterFees()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m;
        var currentBid = 98.5m; // Below stop loss threshold

        // Create bot and trade with fee equal to quantity
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, 0.01m, fee: 0.01m);
        
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify no exit order was placed as no quantity remains after fees
        VerifyExitOrderPlaced(bot, Times.Never());
        
        // Verify trade wasn't updated
        var savedTrade = await DbContext.Trades.FirstAsync();
        Assert.Null(savedTrade.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldIgnoreTradesThatAlreadyHaveExitOrders()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m;
        var currentBid = 98.5m; // Below stop loss threshold

        // Create bot and two trades
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade1 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        var trade2 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        
        // Give trade1 an existing exit order
        var existingExitOrder = CreateOrder(bot, 99m, 0.01m, false, OrderStatus.New);
        trade1.ExitOrder = existingExitOrder;
        await DbContext.SaveChangesAsync();
        
        // Setup exit order mock - should only be called once for trade2
        var newExitOrder = CreateOrder(bot, currentBid, 0.01m, false);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newExitOrder);
        
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order was placed only once with quantity for trade2
        VerifyExitOrderPlaced(
            bot, 
            Times.Once(), 
            expectedQuantity: 0.01m);
            
        // Verify trade1 still has its original exit order
        var updatedTrade1 = await DbContext.Trades
            .Where(t => t.EntryOrder.Id == trade1.EntryOrder.Id)
            .Include(t => t.ExitOrder)
            .FirstAsync();
            
        Assert.Equal(existingExitOrder.Id, updatedTrade1.ExitOrder.Id);
        
        // Verify trade2 has the new exit order
        var updatedTrade2 = await DbContext.Trades
            .Where(t => t.EntryOrder.Id == trade2.EntryOrder.Id)
            .Include(t => t.ExitOrder)
            .FirstAsync();
            
        Assert.Equal(newExitOrder.Id, updatedTrade2.ExitOrder.Id);
    }
    
    [Fact]
    public async Task Handle_ShouldUseAverageFillPrice_WhenAvailable()
    {
        // Arrange
        var entryPrice = 100m;
        var averageFillPrice = 101m; // Different from limit price
        var stopLossPercent = 1.0m;
        
        // Stop loss threshold based on average fill price
        var stopLossPrice = averageFillPrice * (1 - stopLossPercent / 100m); // 99.99
        var currentBid = stopLossPrice - 0.1m; // 99.89 - just below stop loss

        // Create bot and trade
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Create entry order with average fill price
        var entryOrder = CreateOrder(bot, entryPrice, 0.01m, true, OrderStatus.Filled, 0.01m);
        entryOrder.AverageFillPrice = averageFillPrice;
        
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();
        
        // Setup exit order mock
        var exitOrder = CreateOrder(bot, currentBid, 0.01m, false);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exitOrder);
        
        var ticker = CreateTicker(currentBid, currentBid + 0.2m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order was placed (indicates stop loss was calculated using average fill price)
        VerifyExitOrderPlaced(bot, Times.Once());
        
        // Verify trade was updated with exit order
        var savedTrade = await DbContext.Trades.Include(t => t.ExitOrder).FirstAsync();
        Assert.NotNull(savedTrade.ExitOrder);
    }
} 