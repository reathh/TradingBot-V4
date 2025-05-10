using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.PlaceExitOrders;

/// <summary>
/// Tests for advance exit order placement functionality
/// </summary>
public class PlaceExitOrdersAdvanceTests : PlaceExitOrdersTestBase
{
    [Fact]
    public async Task Handle_ShouldPlaceExitOrdersInAdvance_WhenConfigured_LongBot()
    {
        // Arrange
        var bot = await CreateTestBot(
            exitStep: 1.0m,
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            isLong: true);
        
        // Create trades - none are eligible yet as price hasn't moved enough
        var entryPrices = new[] { 100m, 99m, 98m };
        var trades = new List<Trade>();
        
        foreach (var price in entryPrices)
        {
            var trade = await CreateCompletedTrade(bot, price);
            trades.Add(trade);
        }
        
        // Create ticker with price that doesn't reach exit step yet
        var ticker = CreateTestTicker(100.5m, 100.8m); // Below exit threshold for all (100 + 1 = 101)
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup for advance exit orders for each trade
        var advanceOrders = new[]
        {
            (101m, 1m), // Exit for 100 entry
            (100m, 1m), // Exit for 99 entry
            (99m, 1m)   // Exit for 98 entry
        };
        
        SetupExitOrderSequence(bot, advanceOrders);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify each advance order was placed with exact parameters
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 101m),
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
            
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100m),
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
            
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 99m),
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify all trades have exit orders (advance orders)
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => trades.Select(tr => tr.Id).Contains(t.Id))
            .OrderByDescending(t => t.EntryOrder.Price)
            .ToListAsync();
            
        Assert.Equal(3, updatedTrades.Count);
        
        // Verify each trade has its own exit order with correct price
        // First trade (entry 100) should have exit at 101
        Assert.NotNull(updatedTrades[0].ExitOrder);
        Assert.Equal(101m, updatedTrades[0].ExitOrder.Price);
        Assert.Equal(1m, updatedTrades[0].ExitOrder.Quantity);
        
        // Second trade (entry 99) should have exit at 100
        Assert.NotNull(updatedTrades[1].ExitOrder);
        Assert.Equal(100m, updatedTrades[1].ExitOrder.Price);
        Assert.Equal(1m, updatedTrades[1].ExitOrder.Quantity);
        
        // Third trade (entry 98) should have exit at 99
        Assert.NotNull(updatedTrades[2].ExitOrder);
        Assert.Equal(99m, updatedTrades[2].ExitOrder.Price);
        Assert.Equal(1m, updatedTrades[2].ExitOrder.Quantity);
    }
    
    [Fact]
    public async Task Handle_ShouldPlaceExitOrdersInAdvance_WhenSomeTradesAreEligible()
    {
        // Arrange
        var bot = await CreateTestBot(
            exitStep: 1.0m,
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            isLong: true);
        
        // Create trades with different entry prices
        // Only the first trade will be eligible for exit based on current price
        var entryPrices = new[] { 100m, 99m, 98m };
        var trades = new List<Trade>();
        
        foreach (var price in entryPrices)
        {
            var trade = await CreateCompletedTrade(bot, price);
            trades.Add(trade);
        }
        
        // Create ticker with price that reaches exit step only for the first trade
        var ticker = CreateTestTicker(100.5m, 101.1m); // Only trade with entry price 100 is eligible (100 + exitStep <= 101.1)
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup for consolidated exit order for the eligible trade
        // and advance exit orders for trades that aren't yet eligible
        var consolidatedOrder = (101.1m, 1m); // 1 unit for the eligible trade
        var advanceOrders = new[]
        {
            (100m, 1m), // Advance exit for 99 entry
            (99m, 1m)   // Advance exit for 98 entry
        };
        
        // Setup all orders in sequence: first the consolidated, then the advance orders
        var allOrders = new List<(decimal, decimal)> { consolidatedOrder };
        allOrders.AddRange(advanceOrders);
        SetupExitOrderSequence(bot, allOrders.ToArray());
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify exact number of orders placed (1 consolidated + 2 advance)
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
        
        // Verify first eligible trade has exit order at the ask price
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 101.1m),
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify advance orders for non-eligible trades
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100m),
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
            
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 99m),
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify all trades have exit orders
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => trades.Select(tr => tr.Id).Contains(t.Id))
            .OrderByDescending(t => t.EntryOrder.Price)
            .ToListAsync();
            
        Assert.Equal(3, updatedTrades.Count);
        
        // All trades should have exit orders, but with different prices/quantities
        Assert.All(updatedTrades, t => Assert.NotNull(t.ExitOrder));
        
        // First trade (entry 100) should have exit at 101.1 (consolidated order)
        Assert.Equal(101.1m, updatedTrades[0].ExitOrder.Price);
        
        // Second trade (entry 99) should have exit at 100 (advance order)
        Assert.Equal(100m, updatedTrades[1].ExitOrder.Price);
        
        // Third trade (entry 98) should have exit at 99 (advance order)
        Assert.Equal(99m, updatedTrades[2].ExitOrder.Price);
    }
    
    [Fact]
    public async Task Handle_ShouldRespectExitOrdersInAdvanceSetting()
    {
        // Arrange
        var bot = await CreateTestBot(
            exitStep: 1.0m,
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            isLong: true);
            
        // Set a specific ExitOrdersInAdvance value
        bot.ExitOrdersInAdvance = 1; // Only place 1 advance exit order
        await DbContext.SaveChangesAsync();
        
        // Create trades with different entry prices
        // None are eligible for immediate exit (we'll use a price below all exit thresholds)
        var entryPrices = new[] { 100m, 99m, 98m };
        var trades = new List<Trade>();
        
        foreach (var price in entryPrices)
        {
            var trade = await CreateCompletedTrade(bot, price);
            trades.Add(trade);
        }
        
        // Create ticker with price that doesn't reach exit step for ANY trade
        var ticker = CreateTestTicker(98.8m, 98.8m); // Below exit threshold for all trades (98 + 1 = 99 > 98.8)
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup advance exit order - with new logic, the lowest entry price (98m) gets prioritized
        var advanceOrder = (99m, 1m); // Exit price for 98m entry (98 + 1 = 99)
        SetupExitOrderSequence(bot, advanceOrder);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify exactly one order was placed due to ExitOrdersInAdvance=1
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 99m), // Exit price for trade with entry 98m
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Exactly(1));
        
        // Verify only one trade has exit order (the one with lowest entry price)
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => trades.Select(tr => tr.Id).Contains(t.Id))
            .OrderByDescending(t => t.EntryOrder.Price)
            .ToListAsync();
            
        Assert.Equal(3, updatedTrades.Count);
        Assert.Null(updatedTrades[0].ExitOrder);    // First trade (entry 100) has no exit order
        Assert.Null(updatedTrades[1].ExitOrder);    // Second trade (entry 99) has no exit order
        Assert.NotNull(updatedTrades[2].ExitOrder); // Third trade (entry 98) has exit order
    }
    
    [Fact]
    public async Task Handle_ShouldNotPlaceAdvanceExitOrders_WhenNotConfigured()
    {
        // Arrange
        var bot = await CreateTestBot(
            exitStep: 1.0m,
            placeOrdersInAdvance: false, // Explicitly disable advance orders
            ordersInAdvance: 0,
            isLong: true);
        
        // Create trades - none are eligible yet as price hasn't moved enough
        var entryPrices = new[] { 100m, 99m, 98m };
        foreach (var price in entryPrices)
        {
            await CreateCompletedTrade(bot, price);
        }
        
        // Create ticker with price that doesn't reach exit step
        var ticker = CreateTestTicker(100.5m, 100.8m); // Below exit threshold (100 + 1 = 101)
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify no exit orders were placed
        VerifyNoExitOrdersPlaced(bot);
        
        // Verify no trades have exit orders
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => t.Bot.Id == bot.Id)
            .ToListAsync();
            
        Assert.Equal(3, updatedTrades.Count);
        Assert.All(updatedTrades, t => Assert.Null(t.ExitOrder));
    }
    
    [Fact]
    public async Task Handle_ShouldPlaceExitOrdersInAdvance_ForShortBot()
    {
        // Arrange
        var bot = await CreateTestBot(
            exitStep: 1.0m,
            placeOrdersInAdvance: true,
            ordersInAdvance: 3,
            isLong: false);
        
        // Create trades - none are eligible yet as price hasn't moved enough
        var entryPrices = new[] { 101m, 102m, 103m };
        var trades = new List<Trade>();
        
        foreach (var price in entryPrices)
        {
            var trade = await CreateCompletedTrade(bot, price);
            trades.Add(trade);
        }
        
        // Create ticker with price that doesn't reach exit step yet
        var ticker = CreateTestTicker(100.5m, 100.8m); // Above exit threshold for all (101 - 1 = 100)
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup for advance exit orders for each trade - with new logic, highest entry price first for short bots
        var advanceOrders = new[]
        {
            (102m, 1m),  // Exit for 103 entry (highest priority)
            (101m, 1m),  // Exit for 102 entry (second priority)
            (100m, 1m)   // Exit for 101 entry (lowest priority)
        };
        
        SetupExitOrderSequence(bot, advanceOrders);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify each advance order was placed with exact parameters
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 102m), // Exit for 103 entry (highest priority for short bot)
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
            
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 101m), // Exit for 102 entry (second priority)
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
            
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100m), // Exit for 101 entry (lowest priority)
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify all trades have exit orders (advance orders)
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => trades.Select(tr => tr.Id).Contains(t.Id))
            .OrderBy(t => t.EntryOrder.Price)
            .ToListAsync();
            
        Assert.Equal(3, updatedTrades.Count);
        
        // Verify each trade has its own exit order with correct price
        // First trade (entry 101) should have exit at 100
        Assert.NotNull(updatedTrades[0].ExitOrder);
        Assert.Equal(100m, updatedTrades[0].ExitOrder.Price);
        Assert.Equal(1m, updatedTrades[0].ExitOrder.Quantity);
        
        // Second trade (entry 102) should have exit at 101
        Assert.NotNull(updatedTrades[1].ExitOrder);
        Assert.Equal(101m, updatedTrades[1].ExitOrder.Price);
        Assert.Equal(1m, updatedTrades[1].ExitOrder.Quantity);
        
        // Third trade (entry 103) should have exit at 102
        Assert.NotNull(updatedTrades[2].ExitOrder);
        Assert.Equal(102m, updatedTrades[2].ExitOrder.Price);
        Assert.Equal(1m, updatedTrades[2].ExitOrder.Quantity);
    }
}
