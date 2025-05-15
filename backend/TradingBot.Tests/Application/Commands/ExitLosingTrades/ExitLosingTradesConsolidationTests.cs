using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

/// <summary>
/// Tests for the order consolidation functionality in ExitLosingTrades command using FakeItEasy
/// </summary>
public class ExitLosingTradesConsolidationTests : ExitLosingTradesTestBase
{
    [Fact]
    public async Task Handle_ShouldConsolidateMultipleTrades_IntoSingleOrder_ForLongBot()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m; // 1% stop loss
        var stopLossPrice = entryPrice * (1 - stopLossPercent / 100m); // 99.0
        var currentBid = stopLossPrice - 0.1m; // 98.9 - below stop loss
        var currentAsk = currentBid + 0.2m; // 99.1
        
        // Create long bot
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Create multiple trades at the same price
        var trade1 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        var trade2 = await CreateFilledTrade(bot, entryPrice, 0.02m);
        var trade3 = await CreateFilledTrade(bot, entryPrice, 0.03m);
        
        // Total quantity for consolidated order
        var totalQuantity = 0.06m; // 0.01 + 0.02 + 0.03
        
        // Setup exit order for consolidated trades - long exits at ask price
        var exitOrder = CreateOrder(bot, currentAsk, totalQuantity, false);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(totalQuantity),
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
        
        // Verify single consolidated exit order was placed
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(totalQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
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
    public async Task Handle_ShouldConsolidateMultipleTrades_IntoSingleOrder_ForShortBot()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m; // 1% stop loss
        var stopLossPrice = entryPrice * (1 + stopLossPercent / 100m); // 101.0
        var currentAsk = stopLossPrice + 0.1m; // 101.1 - above stop loss
        var currentBid = currentAsk - 0.2m; // 100.9
        
        // Create short bot
        var bot = await CreateStopLossBot(isLong: false, stopLossPercent: stopLossPercent);
        
        // Create multiple trades at the same price
        var trade1 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        var trade2 = await CreateFilledTrade(bot, entryPrice, 0.02m);
        var trade3 = await CreateFilledTrade(bot, entryPrice, 0.03m);
        
        // Total quantity for consolidated order
        var totalQuantity = 0.06m; // 0.01 + 0.02 + 0.03
        
        // Setup exit order for consolidated trades - short exits at bid price
        var exitOrder = CreateOrder(bot, currentBid, totalQuantity, true);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentBid),
                A<decimal>.That.IsEqualTo(totalQuantity),
                A<bool>.That.IsEqualTo(true),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder);
        
        // Create ticker with price above stop loss
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify single consolidated exit order was placed
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentBid),
                A<decimal>.That.IsEqualTo(totalQuantity),
                A<bool>.That.IsEqualTo(true),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
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
    public async Task Handle_ShouldConsolidateTradesWithDifferentEntryPrices()
    {
        // Arrange
        var stopLossPercent = 1.0m;
        var currentBid = 98m;
        var currentAsk = 99m;
        
        // Create bot
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Create trades with different entry prices that all trigger stop loss
        var trade1 = await CreateFilledTrade(bot, 100m, 0.01m); // stop loss at 99
        var trade2 = await CreateFilledTrade(bot, 101m, 0.02m); // stop loss at 99.99
        var trade3 = await CreateFilledTrade(bot, 102m, 0.03m); // stop loss at 100.98
        
        // Current bid of 98 triggers stop loss for all trades
        
        // Total quantity for consolidated order
        var totalQuantity = 0.06m; // 0.01 + 0.02 + 0.03
        
        // Setup exit order
        var exitOrder = CreateOrder(bot, currentAsk, totalQuantity, false);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(totalQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder);
        
        // Create ticker with price below all stop losses
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify single consolidated exit order was placed
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(totalQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
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
    public async Task Handle_ShouldOnlyConsolidateTradesThatExceedStopLoss()
    {
        // Arrange
        var stopLossPercent = 1.0m;
        var currentBid = 98.5m; // Below stop loss for trade1 (99) and trade2 (99.99), but above for trade3 (103.95)
        var currentAsk = 99m;
        
        // Create bot
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Create trades with different entry prices
        var trade1 = await CreateFilledTrade(bot, 100m, 0.01m); // stop loss at 99 - triggered (currentBid < 99)
        var trade2 = await CreateFilledTrade(bot, 101m, 0.02m); // stop loss at 99.99 - triggered (currentBid < 99.99)
        var trade3 = await CreateFilledTrade(bot, 98m, 0.03m); // stop loss at 97.02 - NOT triggered (currentBid > 97.02)
        
        // Calculate stop loss thresholds to verify logic
        var stopLoss1 = 100m * (1 - stopLossPercent / 100m); // 99
        var stopLoss2 = 101m * (1 - stopLossPercent / 100m); // 99.99
        var stopLoss3 = 98m * (1 - stopLossPercent / 100m); // 97.02
        
        // Verify our test setup - only trades 1 and 2 should trigger stop loss
        Assert.True(currentBid < stopLoss1, $"Test setup error: currentBid {currentBid} should be < stopLoss1 {stopLoss1}");
        Assert.True(currentBid < stopLoss2, $"Test setup error: currentBid {currentBid} should be < stopLoss2 {stopLoss2}");
        Assert.True(currentBid > stopLoss3, $"Test setup error: currentBid {currentBid} should be > stopLoss3 {stopLoss3}");
        
        // Only trade1 and trade2 should be exited because only they exceed stop loss threshold
        var triggerQuantity = 0.03m; // 0.01 + 0.02

        // Setup exit order
        var exitOrder = CreateOrder(bot, currentAsk, triggerQuantity, false);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(triggerQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder);
        
        // Create ticker with price below stop loss for first two trades
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order was placed with quantity for trade1 and trade2 only
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(triggerQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        // Verify trade1 and trade2 were updated but not trade3
        var allTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        
        var updatedTrade1 = allTrades.First(t => t.EntryOrder.Price == 100m);
        var updatedTrade2 = allTrades.First(t => t.EntryOrder.Price == 101m);
        var updatedTrade3 = allTrades.First(t => t.EntryOrder.Price == 98m);
        
        Assert.NotNull(updatedTrade1.ExitOrder);
        Assert.NotNull(updatedTrade2.ExitOrder);
        Assert.Null(updatedTrade3.ExitOrder);
    }
    
    [Fact]
    public async Task Handle_ShouldHandleMultipleBotsWithMultipleTrades()
    {
        // Arrange
        var stopLossPercent = 1.0m;
        var currentBid = 98.5m;
        var currentAsk = 99m;
        
        // Create two bots
        var longBot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var shortBot = await CreateStopLossBot(isLong: false, stopLossPercent: stopLossPercent);
        
        // Create trades for longBot
        var longTrade1 = await CreateFilledTrade(longBot, 100m, 0.01m);
        var longTrade2 = await CreateFilledTrade(longBot, 100m, 0.02m);
        
        // Create trades for shortBot with entry price that triggers stop loss
        var shortTrade1 = await CreateFilledTrade(shortBot, 97m, 0.01m); // stop loss at 97.97
        var shortTrade2 = await CreateFilledTrade(shortBot, 97m, 0.02m);
        
        // Setup exit orders for both bots
        var longExitOrder = CreateOrder(longBot, currentAsk, 0.03m, false);
        var shortExitOrder = CreateOrder(shortBot, currentBid, 0.03m, true);
        
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == longBot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(0.03m),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(longExitOrder);
            
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == shortBot.Id),
                A<decimal>.That.IsEqualTo(currentBid),
                A<decimal>.That.IsEqualTo(0.03m),
                A<bool>.That.IsEqualTo(true),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(shortExitOrder);
        
        // Create ticker
        var ticker = CreateTicker(currentBid, currentAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit orders were placed for both bots
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == longBot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(0.03m),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == shortBot.Id),
                A<decimal>.That.IsEqualTo(currentBid),
                A<decimal>.That.IsEqualTo(0.03m),
                A<bool>.That.IsEqualTo(true),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        // Verify all trades were updated correctly
        var longBotTrades = await DbContext.Trades
            .Where(t => t.BotId == longBot.Id)
            .Include(t => t.ExitOrder)
            .ToListAsync();
            
        Assert.Equal(2, longBotTrades.Count);
        foreach (var trade in longBotTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(longExitOrder.Id, trade.ExitOrder.Id);
        }
        
        var shortBotTrades = await DbContext.Trades
            .Where(t => t.BotId == shortBot.Id)
            .Include(t => t.ExitOrder)
            .ToListAsync();
            
        Assert.Equal(2, shortBotTrades.Count);
        foreach (var trade in shortBotTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(shortExitOrder.Id, trade.ExitOrder.Id);
        }
    }
    
    [Fact]
    public async Task Handle_ShouldConsolidateTradesWithFees_AccountingForRounding()
    {
        // Arrange
        var stopLossPercent = 1.0m;
        var currentBid = 98m;
        var currentAsk = 99m;
        
        // Create bot
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Create trades with fees
        var trade1 = await CreateFilledTrade(bot, 100m, 0.01m, fee: 0.0001m);
        var trade2 = await CreateFilledTrade(bot, 100m, 0.02m, fee: 0.0002m);
        var trade3 = await CreateFilledTrade(bot, 100m, 0.03m, fee: 0.0003m);
        
        // Expected quantity after fees (0.01 - 0.0001) + (0.02 - 0.0002) + (0.03 - 0.0003) = 0.0594
        // Assuming the symbolInfo has a step size of 0.0001, the rounded quantity would be 0.0594
        var expectedQuantity = 0.0594m;
        
        // Setup symbol info with step size
        var symbolInfo = new SymbolInfo(0.0001m, 0.0001m, 4); // Min qty 0.0001, step size 0.0001, 4 decimal places
        A.CallTo(() => SymbolInfoCacheFake.GetAsync(A<string>.Ignored, A<CancellationToken>.Ignored))
            .Returns(symbolInfo);
        
        // Setup exit order
        var exitOrder = CreateOrder(bot, currentAsk, expectedQuantity, false);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(expectedQuantity),
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
                A<decimal>.That.IsEqualTo(expectedQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        // Verify all trades were updated with the same exit order
        var savedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        Assert.Equal(3, savedTrades.Count);
        foreach (var trade in savedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(exitOrder.Id, trade.ExitOrder.Id);
        }
    }
} 