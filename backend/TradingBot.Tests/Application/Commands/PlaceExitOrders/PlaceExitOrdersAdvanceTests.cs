using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Data;

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
        
        // Setup for advance exit orders
        // With our new implementation logic, the trades will be prioritized by entry price (lowest first)
        // So the order will be for trades with entries 98, 99, 100
        var advanceOrders = new[]
        {
            (100.8m, 2m), // Consolidate the two lowest-entry trades (98, 99)
            (101.0m, 1m)  // Third advance order for the highest entry price (100)
        };
        
        SetupExitOrderSequence(bot, advanceOrders);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify advance orders were placed
        // First consolidated order for the two lower entry prices
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100.8m),
            It.Is<decimal>(q => q == 2m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
            
        // Second order for the highest entry price
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 101.0m),
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
        
        // Verify all trades have exit orders
        Assert.All(updatedTrades, t => Assert.NotNull(t.ExitOrder));
        
        // Verify exit price for highest entry price (100)
        Assert.Equal(101.0m, updatedTrades[0].ExitOrder!.Price);
        
        // Verify exit price for the two lower entry prices (consolidated)
        var lowerPriceExitOrderId = updatedTrades[1].ExitOrder!.Id;
        Assert.Equal(lowerPriceExitOrderId, updatedTrades[2].ExitOrder!.Id); // Same order for both
        Assert.Equal(100.8m, updatedTrades[1].ExitOrder!.Price);
        Assert.Equal(100.8m, updatedTrades[2].ExitOrder!.Price);
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
        var entryPrices = new[] { 100m, 99m, 98m };
        var trades = new List<Trade>();
        
        foreach (var price in entryPrices)
        {
            var trade = await CreateCompletedTrade(bot, price);
            trades.Add(trade);
        }
        
        // Create ticker with price that makes ALL trades eligible for immediate exit
        var ticker = CreateTestTicker(101.1m, 101.1m); // Ask price > all entry+exitStep thresholds
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup for a single consolidated exit order for all trades (quantity 3)
        var consolidatedOrder = (101.1m, 3m); // All 3 trades at current price
        SetupExitOrderSequence(bot, consolidatedOrder);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify exactly one consolidated order was placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 101.1m), // Current price
            It.Is<decimal>(q => q == 3m),     // Total quantity for all 3 trades
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify no other orders were placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Exactly(1));
        
        // Verify all trades have the same exit order
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => trades.Select(tr => tr.Id).Contains(t.Id))
            .OrderByDescending(t => t.EntryOrder.Price)
            .ToListAsync();
            
        Assert.Equal(3, updatedTrades.Count);
        
        // All trades should have the same exit order
        Assert.All(updatedTrades, t => Assert.NotNull(t.ExitOrder));
        
        // All trades should reference the same exit order with price 101.1
        var exitOrderId = updatedTrades[0].ExitOrder!.Id;
        Assert.Equal(exitOrderId, updatedTrades[1].ExitOrder!.Id);
        Assert.Equal(exitOrderId, updatedTrades[2].ExitOrder!.Id);
        Assert.Equal(101.1m, updatedTrades[0].ExitOrder!.Price);
        Assert.Equal(3m, updatedTrades[0].ExitOrder!.Quantity);
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
        Assert.Equal(100m, updatedTrades[1].ExitOrder!.Price);   // Entry 99m: advance exit at 100m
        Assert.Equal(99.2m, updatedTrades[2].ExitOrder!.Price);  // Entry 98m: consolidated exit at current price
        Assert.Equal(99.2m, updatedTrades[3].ExitOrder!.Price);  // Entry 97m: consolidated exit at current price
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
        
        // Create trades - two are eligible for immediate exit, one isn't
        var entryPrices = new[] { 100m, 99m, 98m };
        var trades = new List<Trade>();
        
        foreach (var price in entryPrices)
        {
            var trade = await CreateCompletedTrade(bot, price);
            trades.Add(trade);
        }
        
        // Create ticker with price that makes 98m and 99m eligible for immediate exit
        var ticker = CreateTestTicker(100.5m, 100.8m); // 98+1 = 99 < 100.8, 99+1 = 100 < 100.8, but 100+1 = 101 > 100.8
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup for consolidated exit order for eligible trades
        var consolidatedOrder = (100.8m, 2m); // 2 units for eligible trades (98m and 99m)
        SetupExitOrderSequence(bot, consolidatedOrder);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify consolidated order was placed for eligible trades
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100.8m),
            It.Is<decimal>(q => q == 2m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify only eligible trades have exit orders
        var updatedTrades = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .Where(t => trades.Select(tr => tr.Id).Contains(t.Id))
            .OrderByDescending(t => t.EntryOrder.Price)
            .ToListAsync();
            
        Assert.Equal(3, updatedTrades.Count);
        Assert.Null(updatedTrades[0].ExitOrder);  // Trade with entry 100m has no exit (not eligible yet)
        Assert.NotNull(updatedTrades[1].ExitOrder); // Trade with entry 99m has exit
        Assert.NotNull(updatedTrades[2].ExitOrder); // Trade with entry 98m has exit
        
        // Both eligible trades should have the same exit order
        var exitOrderId = updatedTrades[1].ExitOrder!.Id;
        Assert.Equal(exitOrderId, updatedTrades[2].ExitOrder!.Id);
        Assert.Equal(100.8m, updatedTrades[1].ExitOrder!.Price);
        Assert.Equal(2m, updatedTrades[1].ExitOrder!.Quantity);
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
        
        // Setup for advance exit orders - with new consolidation logic
        // For short bot, we prioritize higher entry prices first
        var advanceOrders = new[]
        {
            (100.5m, 2m), // Consolidated order for entries 103m and 102m (higher prices first)
            (100.0m, 1m)  // Individual order for entry 101m
        };
        
        SetupExitOrderSequence(bot, advanceOrders);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - verify orders were placed
        // First order is consolidated for higher entry prices
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100.5m),
            It.Is<decimal>(q => q == 2m),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);
            
        // Second order for the lower entry price
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == 100.0m),
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
        
        // Verify all trades have exit orders
        Assert.All(updatedTrades, t => Assert.NotNull(t.ExitOrder));
        
        // Trade with entry 101m has individual exit order
        Assert.Equal(100.0m, updatedTrades[0].ExitOrder!.Price);
        
        // Trades with entries 102m and 103m share consolidated exit order
        var higherEntriesOrderId = updatedTrades[1].ExitOrder!.Id;
        Assert.Equal(higherEntriesOrderId, updatedTrades[2].ExitOrder!.Id);
        Assert.Equal(100.5m, updatedTrades[1].ExitOrder!.Price);
        Assert.Equal(100.5m, updatedTrades[2].ExitOrder!.Price);
    }
}
