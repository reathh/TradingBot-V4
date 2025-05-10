using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.PlaceExitOrders;

/// <summary>
/// Tests for consolidated exit order functionality
/// </summary>
public class PlaceExitOrdersConsolidatedTests : PlaceExitOrdersTestBase
{
    [Fact]
    public async Task Handle_ShouldPlaceConsolidatedExitOrder_ForMultipleTradesWithLongBot()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        
        // Create multiple trades at different entry prices
        var entryPrices = new[] { 100m, 99m, 98m };
        var trades = new List<Trade>();
        
        foreach (var price in entryPrices)
        {
            var trade = await CreateCompletedTrade(bot, price);
            trades.Add(trade);
        }
        
        // Create ticker with price that reaches exit step for all trades
        var ticker = CreateTestTicker(100.5m, 101m); // Exit at 101 for long (entry + exitStep for the first trade)
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup consolidated exit order for all eligible trades (3 units total)
        var consolidatedPrice = 101m; // Exactly at the ask price
        var consolidatedQuantity = 3m; // 1 unit for each trade
        SetupConsolidatedExitOrder(bot, consolidatedPrice, consolidatedQuantity);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify one consolidated order was placed with exact parameters
        VerifyExitOrderPlaced(bot, consolidatedPrice, consolidatedQuantity);
        
        // Verify all trades have the same exit order
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => trades.Select(t => t.Id).Contains(t.Id))
            .ToListAsync();
        
        Assert.Equal(3, updatedTrades.Count);
        
        // All trades should reference the same exit order
        Assert.NotNull(updatedTrades.First().ExitOrder);
        var exitOrderId = updatedTrades.First().ExitOrder!.Id;
        
        foreach (var trade in updatedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(exitOrderId, trade.ExitOrder!.Id);
            Assert.Equal(consolidatedPrice, trade.ExitOrder!.Price);
            Assert.Equal(consolidatedQuantity, trade.ExitOrder!.Quantity);
            Assert.False(trade.ExitOrder!.IsBuy); // Selling for long positions
        }
    }
    
    [Fact]
    public async Task Handle_ShouldPlaceConsolidatedExitOrder_ForMultipleTradesWithShortBot()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: false);
        
        // Create multiple trades at different entry prices
        var entryPrices = new[] { 101m, 102m, 103m };
        var trades = new List<Trade>();
        
        foreach (var price in entryPrices)
        {
            var trade = await CreateCompletedTrade(bot, price);
            trades.Add(trade);
        }
        
        // Create ticker with price that reaches exit step for all trades
        var ticker = CreateTestTicker(100m, 100.5m); // Exit at 100 for short (entry - exitStep)
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup consolidated exit order for all eligible trades (3 units total)
        var consolidatedPrice = 100m; // Exactly at the bid price
        var consolidatedQuantity = 3m; // 1 unit for each trade
        SetupConsolidatedExitOrder(bot, consolidatedPrice, consolidatedQuantity);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify one consolidated order was placed with exact parameters
        VerifyExitOrderPlaced(bot, consolidatedPrice, consolidatedQuantity);
        
        // Verify all trades have the same exit order
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => trades.Select(t => t.Id).Contains(t.Id))
            .ToListAsync();
        
        Assert.Equal(3, updatedTrades.Count);
        
        // All trades should reference the same exit order
        Assert.NotNull(updatedTrades.First().ExitOrder);
        var exitOrderId = updatedTrades.First().ExitOrder!.Id;
        
        foreach (var trade in updatedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(exitOrderId, trade.ExitOrder!.Id);
            Assert.Equal(consolidatedPrice, trade.ExitOrder!.Price);
            Assert.Equal(consolidatedQuantity, trade.ExitOrder!.Quantity);
            Assert.True(trade.ExitOrder!.IsBuy); // Buying for short positions
        }
    }
    
    [Fact]
    public async Task Handle_ShouldPlaceConsolidatedExitOrderWithCorrectQuantity_WhenTradesHaveDifferentQuantities()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        
        // Create multiple trades with different quantities
        // First trade: entry price = 100, quantity = 1
        var firstEntryOrder = CreateOrder(bot, 100m, 1m, bot.IsLong);
        firstEntryOrder.Closed = true;
        firstEntryOrder.QuantityFilled = 1m;
        var firstTrade = new Trade(firstEntryOrder);
        bot.Trades.Add(firstTrade);
        
        // Second trade: entry price = 99, quantity = 2
        var secondEntryOrder = CreateOrder(bot, 99m, 2m, bot.IsLong);
        secondEntryOrder.Closed = true;
        secondEntryOrder.QuantityFilled = 2m;
        var secondTrade = new Trade(secondEntryOrder);
        bot.Trades.Add(secondTrade);
        
        // Third trade: entry price = 98, quantity = 3
        var thirdEntryOrder = CreateOrder(bot, 98m, 3m, bot.IsLong);
        thirdEntryOrder.Closed = true;
        thirdEntryOrder.QuantityFilled = 3m;
        var thirdTrade = new Trade(thirdEntryOrder);
        bot.Trades.Add(thirdTrade);
        
        await DbContext.SaveChangesAsync();
        
        // Create ticker with price that reaches exit step for all trades
        var ticker = CreateTestTicker(100.5m, 101m); // Exit at 101 for long
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup consolidated exit order with total quantity of all trades (1 + 2 + 3 = 6)
        var consolidatedPrice = 101m;
        var consolidatedQuantity = 6m; // Total of all quantities
        SetupConsolidatedExitOrder(bot, consolidatedPrice, consolidatedQuantity);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify consolidated order was placed with sum of all quantities
        VerifyExitOrderPlaced(bot, consolidatedPrice, consolidatedQuantity);
        
        // Verify all trades have the same exit order with the consolidated quantity
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => t.Bot.Id == bot.Id)
            .ToListAsync();
        
        Assert.Equal(3, updatedTrades.Count);
        
        // All trades should reference the same exit order with the correct total quantity
        Assert.NotNull(updatedTrades.First().ExitOrder);
        var exitOrderId = updatedTrades.First().ExitOrder!.Id;
        
        foreach (var trade in updatedTrades)
        {
            Assert.NotNull(trade.ExitOrder);
            Assert.Equal(exitOrderId, trade.ExitOrder!.Id);
            Assert.Equal(consolidatedPrice, trade.ExitOrder!.Price);
            Assert.Equal(consolidatedQuantity, trade.ExitOrder!.Quantity);
        }
    }
    
    [Fact]
    public async Task Handle_ShouldPlaceConsolidatedExitOrder_ForPartiallyEligibleTrades()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        
        // Create trades with different entry prices
        // First trade with price 100 will be eligible (entry + exitStep = 101 <= ask 101.1)
        var firstTrade = await CreateCompletedTrade(bot, 100m);
        
        // Second trade with price 101 is NOT eligible in our implementation
        // (entry + exitStep = 102 > ask 101.1)
        var secondTrade = await CreateCompletedTrade(bot, 101m);
        
        // Third trade with price 102 will not be eligible (entry + exitStep = 103 > ask 101.1)
        var thirdTrade = await CreateCompletedTrade(bot, 102m);
        
        // Create ticker with price that reaches exit step only for first trade
        var ticker = CreateTestTicker(100.9m, 101.1m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup consolidated exit order ONLY for the first trade (quantity 1)
        var consolidatedPrice = 101.1m; // Exactly at the ask price
        var consolidatedQuantity = 1m; // 1 unit ONLY for the first eligible trade
        SetupConsolidatedExitOrder(bot, consolidatedPrice, consolidatedQuantity);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify one consolidated order was placed with exact parameters for eligible trade
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == consolidatedPrice),
            It.Is<decimal>(q => q == consolidatedQuantity),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify only eligible trade has an exit order
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => t.Id == firstTrade.Id || t.Id == secondTrade.Id || t.Id == thirdTrade.Id)
            .OrderBy(t => t.EntryOrder.Price)
            .ToListAsync();
        
        Assert.Equal(3, updatedTrades.Count);
        
        // Only first trade (entry 100) should have exit order
        Assert.NotNull(updatedTrades[0].ExitOrder);
        Assert.Equal(consolidatedPrice, updatedTrades[0].ExitOrder!.Price);
        Assert.Equal(consolidatedQuantity, updatedTrades[0].ExitOrder!.Quantity);
        
        // Other trades should not have exit orders
        Assert.Null(updatedTrades[1].ExitOrder); // entry 101
        Assert.Null(updatedTrades[2].ExitOrder); // entry 102
    }
    
    [Fact]
    public async Task Handle_ShouldPlaceExitOrders_ForMultipleBots_ThatMeetCriteria()
    {
        // Arrange
        var bot1 = await CreateTestBot(exitStep: 1.0m, entryQuantity: 1m, isLong: true);
        var bot2 = await CreateTestBot(exitStep: 1.0m, entryQuantity: 2m, isLong: true);
        
        // Create a disabled bot
        var disabledBot = await CreateTestBot(exitStep: 1.0m, entryQuantity: 3m, isLong: true);
        disabledBot.Enabled = false;
        await DbContext.SaveChangesAsync();
        
        // Create trades for each bot
        await CreateCompletedTrade(bot1, 100m);
        await CreateCompletedTrade(bot2, 100m);
        await CreateCompletedTrade(disabledBot, 100m);
        
        // Create ticker with price that reaches exit step
        var ticker = CreateTestTicker(100.5m, 101m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup exit orders for each bot
        SetupExitOrderSequence(bot1, (101m, 1m), (101m, 2m));
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - orders placed only for enabled bots
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot1.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
            
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot2.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
            
        // No order for disabled bot
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == disabledBot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
