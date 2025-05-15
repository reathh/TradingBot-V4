using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

/// <summary>
/// Tests for edge cases in ExitLosingTrades command using FakeItEasy
/// </summary>
public class ExitLosingTradesEdgeCaseTests : ExitLosingTradesTestBase
{
    [Fact]
    public async Task Handle_ShouldHandleNoBotsWithLossTrades()
    {
        // Arrange - No bots with trades
        var ticker = CreateTicker(100m, 101m);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify no API calls were made
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.Ignored,
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustNotHaveHappened();
    }
    
    [Fact]
    public async Task Handle_ShouldHandleTradeBelowMinimumQuantity()
    {
        // Arrange
        var entryPrice = 100m;
        var currentBid = 98m;
        var currentAsk = 102m;
        var stopLossPercent = 1.0m;
        
        // Create bot and trade with small quantity
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, 0.000001m); // Very small quantity
        
        // Set minimum quantity above the trade quantity
        var symbolInfo = new SymbolInfo(0.001m, 0.001m, 5); // Min quantity is 0.001
        A.CallTo(() => SymbolInfoCacheFake.GetAsync(A<string>.Ignored, A<CancellationToken>.Ignored))
            .Returns(symbolInfo);
        
        // Create ticker with price below stop loss
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify no exit order was placed due to minimum quantity constraint
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.Ignored,
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustNotHaveHappened();
        
        // Verify trade was not updated
        var savedTrade = await DbContext.Trades.FirstAsync();
        Assert.Null(savedTrade.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleEntryFees_AndRoundDown()
    {
        // Arrange
        var entryPrice = 100m;
        var currentBid = 99m;
        var currentAsk = 101m;
        var stopLossPercent = 1.0m;
        var entryQty = 1.0m;
        var fee = 0.001m; // 0.1% fee
        
        // Create bot and trade with fee
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent, entryQuantity: entryQty);
        var trade = await CreateFilledTrade(bot, entryPrice, entryQty, fee);
        
        // Expected exit quantity should be entry quantity minus fee (1.0 - 0.001 = 0.999), rounded down to step
        var expectedExitQty = 0.999m;
        
        // Set symbol info with step size
        var symbolInfo = new SymbolInfo(0.001m, 0.001m, 3); // Step size 0.001, 3 decimal places
        A.CallTo(() => SymbolInfoCacheFake.GetAsync(A<string>.Ignored, A<CancellationToken>.Ignored))
            .Returns(symbolInfo);
        
        // Setup exit order
        var exitOrder = CreateOrder(bot, currentAsk, expectedExitQty, false);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(expectedExitQty),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder);
        
        // Create ticker with price below stop loss
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order was placed with fee-adjusted quantity
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(expectedExitQty),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
        
        // Verify trade was updated
        var savedTrade = await DbContext.Trades.Include(t => t.ExitOrder).FirstAsync();
        Assert.NotNull(savedTrade.ExitOrder);
        Assert.Equal(exitOrder.Id, savedTrade.ExitOrder.Id);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleErrorDuringOrderPlacement()
    {
        // Arrange
        var entryPrice = 100m;
        var currentBid = 99m;
        var currentAsk = 101m;
        var stopLossPercent = 1.0m;
        
        // Create bot and trade
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, bot.EntryQuantity);
        
        // Setup API to throw exception
        var errorMessage = "Insufficient funds";
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.Ignored,
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Throws(new Exception(errorMessage));
        
        // Create ticker with price below stop loss
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(errorMessage, result.Errors.First());
        
        // Verify API call was attempted
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.Ignored,
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappened();
        
        // Verify trade was not updated
        var savedTrade = await DbContext.Trades.FirstAsync();
        Assert.Null(savedTrade.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldProcessAllBots_EvenWhenSomeFail()
    {
        // Arrange - Create multiple bots with one that will fail
        var stopLossPercent = 1.0m;
        var entryPrice = 100m;
        var currentBid = 99m;
        var currentAsk = 101m;
        
        // Create bots, one at a time with explicit IDs
        var bot1 = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var bot2 = await CreateStopLossBot(isLong: false, stopLossPercent: stopLossPercent);
        var bot3 = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Add trades to each bot
        var trade1 = await CreateFilledTrade(bot1, entryPrice, bot1.EntryQuantity);
        var trade2 = await CreateFilledTrade(bot2, entryPrice, bot2.EntryQuantity);
        var trade3 = await CreateFilledTrade(bot3, entryPrice, bot3.EntryQuantity);
        
        var bots = new List<Bot> { bot1, bot2, bot3 };
        
        // Setup exit orders - bot1 and bot2 succeed, but bot3 fails
        var exitOrder1 = CreateOrder(bot1, currentAsk, bot1.EntryQuantity, false);
        var exitOrder2 = CreateOrder(bot2, currentBid, bot2.EntryQuantity, true);
        
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot1.Id),
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder1);
            
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot2.Id),
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder2);
            
        // Bot3 throws exception
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot3.Id),
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Throws(new Exception("Simulated error"));
        
        // Create ticker that will trigger stop loss for all bots
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert - Command will fail because bot3 failed
        Assert.False(result.Succeeded);
        Assert.Contains("Failed to place loss-exit order for bot", result.Errors.First());
        
        // Verify exit order was attempted for all bots
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot1.Id),
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot2.Id),
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot3.Id),
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
        
        // Verify successfully processed bot trades were updated but failed bot was not
        var bot1Trade = await DbContext.Trades.Where(t => t.BotId == bot1.Id).Include(t => t.ExitOrder).FirstAsync();
        var bot2Trade = await DbContext.Trades.Where(t => t.BotId == bot2.Id).Include(t => t.ExitOrder).FirstAsync();
        var bot3Trade = await DbContext.Trades.Where(t => t.BotId == bot3.Id).Include(t => t.ExitOrder).FirstAsync();
        
        Assert.NotNull(bot1Trade.ExitOrder);
        Assert.NotNull(bot2Trade.ExitOrder);
        Assert.Null(bot3Trade.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleTradeWithExistingExitOrder()
    {
        // Arrange
        var entryPrice = 100m;
        var currentBid = 99m;
        var currentAsk = 101m;
        var stopLossPercent = 1.0m;
        
        // Create bot with two trades - one with exit order, one without
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Create first trade with existing exit order
        var trade1 = await CreateFilledTrade(bot, entryPrice, bot.EntryQuantity);
        var existingExitOrder = CreateOrder(bot, 98m, bot.EntryQuantity, false);
        trade1.ExitOrder = existingExitOrder;
        
        // Create second trade without exit order
        var trade2 = await CreateFilledTrade(bot, entryPrice, bot.EntryQuantity);
        
        await DbContext.SaveChangesAsync();
        
        // Setup exit order for the second trade
        var exitOrder = CreateOrder(bot, currentAsk, bot.EntryQuantity, false);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(bot.EntryQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder);
        
        // Create ticker that will trigger stop loss
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order was placed for the second trade only
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(bot.EntryQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
        
        // Verify both trades now have exit orders
        var trades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        Assert.Equal(2, trades.Count);
        Assert.All(trades, t => Assert.NotNull(t.ExitOrder));
        
        // First trade should still have the original exit order
        var updatedTrade1 = trades.First(t => t.Id == trade1.Id);
        Assert.Equal(existingExitOrder.Id, updatedTrade1.ExitOrder.Id);
        
        // Second trade should have the new exit order
        var updatedTrade2 = trades.First(t => t.Id == trade2.Id);
        Assert.Equal(exitOrder.Id, updatedTrade2.ExitOrder.Id);
    }
} 