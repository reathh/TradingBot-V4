using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

/// <summary>
/// Tests for ExitLosingTrades command in various market conditions using FakeItEasy
/// </summary>
public class ExitLosingTradesMarketTests : ExitLosingTradesTestBase
{
    [Fact]
    public async Task Handle_ShouldExitLongTrades_DuringFlashCrash()
    {
        // Arrange - simulating a flash crash with price dropping 10%
        var entryPrice = 50000m; // BTC price in USD
        var crashedBid = entryPrice * 0.9m; // 10% flash crash
        var crashedAsk = crashedBid * 1.001m; // Slightly higher ask price
        var stopLossPercent = 1.0m; // 1% stop loss
        
        // Create bot with different trades at various price points
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        
        // Create trades at different entry prices that will all hit stop loss
        var trade1 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        var trade2 = await CreateFilledTrade(bot, entryPrice * 0.98m, 0.01m);
        var trade3 = await CreateFilledTrade(bot, entryPrice * 0.96m, 0.01m);
        var trade4 = await CreateFilledTrade(bot, entryPrice * 0.94m, 0.01m);
        
        var totalQuantity = 0.04m;
        
        // Setup exit order for consolidated trades - long exits at ask price
        var exitOrder = CreateOrder(bot, crashedAsk, totalQuantity, false);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(crashedAsk),
                A<decimal>.That.IsEqualTo(totalQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder);
        
        // Create ticker with flash crash price
        var ticker = CreateTicker(crashedBid, crashedAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify one consolidated exit order was placed with all quantities
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(crashedAsk),
                A<decimal>.That.IsEqualTo(totalQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        // Verify all trades were updated with the exit order
        var savedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        Assert.Equal(4, savedTrades.Count);
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
        var squeezedAsk = entryPrice * 1.1m; // 10% short squeeze
        var squeezedBid = squeezedAsk * 0.999m; // Slightly lower bid price
        var stopLossPercent = 1.0m; // 1% stop loss
        
        // Create bot with different trades at various price points
        var bot = await CreateStopLossBot(isLong: false, stopLossPercent: stopLossPercent);
        
        // Create trades at different entry prices that will all hit stop loss
        var trade1 = await CreateFilledTrade(bot, entryPrice, 0.01m);
        var trade2 = await CreateFilledTrade(bot, entryPrice * 1.02m, 0.01m);
        var trade3 = await CreateFilledTrade(bot, entryPrice * 1.04m, 0.01m);
        var trade4 = await CreateFilledTrade(bot, entryPrice * 1.06m, 0.01m);
        
        var totalQuantity = 0.04m;
        
        // Setup exit order - short exits at bid price
        var exitOrder = CreateOrder(bot, squeezedBid, totalQuantity, true);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(squeezedBid),
                A<decimal>.That.IsEqualTo(totalQuantity),
                A<bool>.That.IsEqualTo(true),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder);
        
        // Create ticker with short squeeze price
        var ticker = CreateTicker(squeezedBid, squeezedAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify one consolidated exit order was placed with all quantities
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(squeezedBid),
                A<decimal>.That.IsEqualTo(totalQuantity),
                A<bool>.That.IsEqualTo(true),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        // Verify all trades were updated with the exit order
        var savedTrades = await DbContext.Trades.Include(t => t.ExitOrder).ToListAsync();
        Assert.Equal(4, savedTrades.Count);
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
        var crashedBid = entryPrice * 0.8m; // 20% drop
        var crashedAsk = crashedBid * 1.001m; // Slightly higher ask price
        var stopLossPercent = 15.0m; // 15% stop loss - large but still triggered by 20% drop
        
        // Create bot with large stop loss
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, 0.01m);
        
        // Setup exit order
        var exitOrder = CreateOrder(bot, crashedAsk, 0.01m, false);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(crashedAsk),
                A<decimal>.That.IsEqualTo(0.01m),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder);
        
        // Create ticker with crash price
        var ticker = CreateTicker(crashedBid, crashedAsk);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify exit order was placed because price moved more than stop loss
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(crashedAsk),
                A<decimal>.That.IsEqualTo(0.01m),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        // Verify trade was updated with exit order
        var savedTrade = await DbContext.Trades.Include(t => t.ExitOrder).FirstAsync();
        Assert.NotNull(savedTrade.ExitOrder);
        Assert.Equal(exitOrder.Id, savedTrade.ExitOrder.Id);
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
        
        // Setup exit orders for both bots - long exits at ask, short exits at bid
        var longExitOrder = CreateOrder(longBot, askPrice, 0.01m, false);
        var shortExitOrder = CreateOrder(shortBot, bidPrice, 0.01m, true);
        
        // Configure fake to return different orders based on bot ID
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == longBot.Id),
                A<decimal>.That.IsEqualTo(askPrice),
                A<decimal>.That.IsEqualTo(0.01m),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(longExitOrder);
            
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == shortBot.Id),
                A<decimal>.That.IsEqualTo(bidPrice),
                A<decimal>.That.IsEqualTo(0.01m),
                A<bool>.That.IsEqualTo(true),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(shortExitOrder);
        
        // Create ticker with wide spread
        var ticker = CreateTicker(bidPrice, askPrice);
        var command = new ExitLosingTradesCommand { Ticker = ticker };

        // Act
        var result = await Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify both bots placed exit orders at the appropriate side of the spread
        // Long bot sells at ask, short bot buys at bid
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == longBot.Id),
                A<decimal>.That.IsEqualTo(askPrice),
                A<decimal>.That.IsEqualTo(0.01m),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == shortBot.Id),
                A<decimal>.That.IsEqualTo(bidPrice),
                A<decimal>.That.IsEqualTo(0.01m),
                A<bool>.That.IsEqualTo(true),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        // Verify both trades were updated with exit orders
        var longBotTrades = await DbContext.Trades.Where(t => t.BotId == longBot.Id).Include(t => t.ExitOrder).ToListAsync();
        var shortBotTrades = await DbContext.Trades.Where(t => t.BotId == shortBot.Id).Include(t => t.ExitOrder).ToListAsync();
        
        Assert.NotNull(longBotTrades.Single().ExitOrder);
        Assert.Equal(longExitOrder.Id, longBotTrades.Single().ExitOrder.Id);
        
        Assert.NotNull(shortBotTrades.Single().ExitOrder);
        Assert.Equal(shortExitOrder.Id, shortBotTrades.Single().ExitOrder.Id);
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
        
        // No order yet, as first ticker won't trigger stop loss
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.Ignored,
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustNotHaveHappened();
        
        // First ticker update - price just above stop loss
        var ticker1 = CreateTicker(stopLossPrice + 10m, stopLossPrice + 20m);
        var command1 = new ExitLosingTradesCommand { Ticker = ticker1 };
        
        // For second ticker, setup exit order since it will trigger stop loss
        var crashedBid = stopLossPrice - 10m;
        var crashedAsk = stopLossPrice;
        var exitOrder = CreateOrder(bot, crashedAsk, 0.01m, false);
        
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(crashedAsk),
                A<decimal>.That.IsEqualTo(0.01m),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(exitOrder);
        
        // Second ticker update - price dropped below stop loss
        var ticker2 = CreateTicker(crashedBid, crashedAsk);
        var command2 = new ExitLosingTradesCommand { Ticker = ticker2 };

        // Act - Process first ticker update (no stop loss)
        var result1 = await Handle(command1, CancellationToken.None);
        
        // Verify no exit order was placed yet
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.Ignored,
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustNotHaveHappened();
        
        // Process second ticker update (should trigger stop loss)
        var result2 = await Handle(command2, CancellationToken.None);

        // Assert
        Assert.True(result1.Succeeded);
        Assert.True(result2.Succeeded);
        
        // Verify exit order was placed with second ticker
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(crashedAsk),
                A<decimal>.That.IsEqualTo(0.01m),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        // Verify trade was updated with exit order
        var savedTrade = await DbContext.Trades.Include(t => t.ExitOrder).FirstAsync();
        Assert.NotNull(savedTrade.ExitOrder);
        Assert.Equal(exitOrder.Id, savedTrade.ExitOrder.Id);
    }
    
    [Fact]
    public async Task Handle_ShouldUseAverageFillPrice_ForStopLossCalculation_WhenAvailable()
    {
        // Arrange - Test using average fill price instead of order price for stop loss calculation
        var entryOrderPrice = 50000m;
        var entryAverageFillPrice = 49800m; // Filled at better price than order price
        var stopLossPercent = 1.0m; // 1% stop loss
        
        // Stop loss should be calculated based on average fill price, not order price
        var stopLossPrice = entryAverageFillPrice * (1 - stopLossPercent / 100m); // 49,302
        
        // Current prices just below stop loss threshold
        var currentBid = stopLossPrice - 10m; // 49,292
        var currentAsk = stopLossPrice; // 49,302
        
        // Create bot and trade with average fill price different from order price
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var entryOrder = CreateOrder(
            bot, 
            entryOrderPrice, 
            bot.EntryQuantity, 
            true, 
            OrderStatus.Filled, 
            bot.EntryQuantity);
        entryOrder.AverageFillPrice = entryAverageFillPrice;
        
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();
        
        // Setup exit order 
        var exitOrder = CreateOrder(bot, currentAsk, bot.EntryQuantity, false);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(bot.EntryQuantity),
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
        
        // Verify exit order was placed because price moved below stop loss calculated from average fill price
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(currentAsk),
                A<decimal>.That.IsEqualTo(bot.EntryQuantity),
                A<bool>.That.IsEqualTo(false),
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
            
        // Verify trade was updated with exit order
        var savedTrade = await DbContext.Trades.Include(t => t.ExitOrder).FirstAsync();
        Assert.NotNull(savedTrade.ExitOrder);
        Assert.Equal(exitOrder.Id, savedTrade.ExitOrder.Id);
    }
} 