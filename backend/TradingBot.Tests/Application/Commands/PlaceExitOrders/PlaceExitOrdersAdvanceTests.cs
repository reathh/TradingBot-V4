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
        var entryPrices = new[] { 100m, 99m, 98m, 97m };
        var trades = new List<Trade>();
        
        foreach (var price in entryPrices)
        {
            var trade = await CreateCompletedTrade(bot, price);
            trades.Add(trade);
        }
        
        // Create ticker with price that:
        // - Reaches exit step for entries 97m and 98m (eligible for immediate exit)
        // - Doesn't reach exit step for entries 99m and 100m (candidates for advance orders)
        var ticker = CreateTestTicker(99.2m, 99.2m); // Above threshold for 97m and 98m entries, below for 99m and 100m
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup for orders:
        // 1. Consolidated order for entries 97m and 98m
        // 2. ONE advance order for entry 99m (since it's closest to being eligible)
        // Note: No advance order for entry 100m as ExitOrdersInAdvance=1
        var orders = new[]
        {
            (99.2m, 2m), // Consolidated order for entries 97m and 98m (exits at current price)
            (100m, 1m)   // Advance order for entry 99m (99 + 1 = 100)
        };
        
        SetupExitOrderSequence(bot, orders);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify exactly two orders were placed
        // First, the consolidated order
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 99.2m), // Consolidated exit at current price
            It.Is<decimal>(q => q == 2m),    // For 2 trades (entries 97m and 98m)
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
            
        // Then, the ONE advance order for entry 99m
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100m),  // Advance exit for entry 99m
            It.Is<decimal>(q => q == 1m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify no other orders were placed (exactly 2 orders total)
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        
        // Verify which trades have exit orders
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => trades.Select(tr => tr.Id).Contains(t.Id))
            .OrderByDescending(t => t.EntryOrder.Price)
            .ToListAsync();
            
        Assert.Equal(4, updatedTrades.Count);
        Assert.Null(updatedTrades[0].ExitOrder);     // Entry 100m has NO exit order (not eligible for advance because of ExitOrdersInAdvance=1)
        Assert.NotNull(updatedTrades[1].ExitOrder);  // Entry 99m has advance exit order
        Assert.NotNull(updatedTrades[2].ExitOrder);  // Entry 98m has consolidated exit order
        Assert.NotNull(updatedTrades[3].ExitOrder);  // Entry 97m has consolidated exit order
        
        // Verify correct exit prices
        Assert.Equal(100m, updatedTrades[1].ExitOrder.Price);   // Entry 99m: advance exit at 100m
        Assert.Equal(99.2m, updatedTrades[2].ExitOrder.Price);  // Entry 98m: consolidated exit at current price
        Assert.Equal(99.2m, updatedTrades[3].ExitOrder.Price);  // Entry 97m: consolidated exit at current price
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
