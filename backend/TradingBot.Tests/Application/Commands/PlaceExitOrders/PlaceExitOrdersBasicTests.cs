using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Data;

namespace TradingBot.Tests.Application.Commands.PlaceExitOrders;

/// <summary>
/// Tests for basic exit order placement functionality
/// </summary>
public class PlaceExitOrdersBasicTests : PlaceExitOrdersTestBase
{
    [Fact]
    public async Task Handle_ShouldPlaceExitOrder_WhenPriceReachesExitStep_LongBot()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        var trade = await CreateCompletedTrade(bot, entryPrice);
        
        // Create ticker with price that reaches exit step
        var exitPrice = 101m; // Up for long
        var ticker = CreateTestTicker(100.5m, 101m); // Bid is 100.5, Ask is 101
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup mock for exit order
        SetupConsolidatedExitOrder(bot, exitPrice, bot.EntryQuantity);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert
        VerifyExitOrderPlaced(bot, exitPrice, bot.EntryQuantity);
        
        // Verify trade was updated with exit order
        var updatedTrade = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .FirstOrDefaultAsync(t => t.Id == trade.Id);
            
        Assert.NotNull(updatedTrade);
        Assert.NotNull(updatedTrade.ExitOrder);
        Assert.Equal(exitPrice, updatedTrade.ExitOrder.Price);
        Assert.Equal(bot.EntryQuantity, updatedTrade.ExitOrder.Quantity);
        Assert.Equal(!bot.IsLong, updatedTrade.ExitOrder.IsBuy);
    }
    
    [Fact]
    public async Task Handle_ShouldPlaceExitOrder_WhenPriceReachesExitStep_ShortBot()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: false);
        var entryPrice = 101m;
        var trade = await CreateCompletedTrade(bot, entryPrice);
        
        // Create ticker with price that reaches exit step
        var exitPrice = 100m; // Down for short
        var ticker = CreateTestTicker(100m, 100.5m); // Bid is 100, Ask is 100.5
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup mock for exit order
        SetupConsolidatedExitOrder(bot, exitPrice, bot.EntryQuantity);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert
        VerifyExitOrderPlaced(bot, exitPrice, bot.EntryQuantity);
        
        // Verify trade was updated with exit order
        var updatedTrade = await DbContext.Trades
            .Include(t => t.ExitOrder)
            .FirstOrDefaultAsync(t => t.Id == trade.Id);
            
        Assert.NotNull(updatedTrade);
        Assert.NotNull(updatedTrade.ExitOrder);
        Assert.Equal(exitPrice, updatedTrade.ExitOrder.Price);
        Assert.Equal(bot.EntryQuantity, updatedTrade.ExitOrder.Quantity);
        Assert.Equal(!bot.IsLong, updatedTrade.ExitOrder.IsBuy);
    }
    
    [Fact]
    public async Task Handle_ShouldNotPlaceExitOrder_WhenPriceIsLowerThanExitStep_LongBot()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        await CreateCompletedTrade(bot, entryPrice);
        
        // Create ticker with price that doesn't reach exit step
        var ticker = CreateTestTicker(100.5m, 100.8m); // Both below exit price of 101
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert
        VerifyNoExitOrdersPlaced(bot);
    }
    
    [Fact]
    public async Task Handle_ShouldNotPlaceExitOrder_WhenPriceIsHigherThanExitStep_ShortBot()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: false);
        var entryPrice = 101m;
        await CreateCompletedTrade(bot, entryPrice);
        
        // Create ticker with price that doesn't reach exit step
        var ticker = CreateTestTicker(100.3m, 100.5m); // Both above exit price of 100
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert
        VerifyNoExitOrdersPlaced(bot);
    }
    
    [Fact]
    public async Task Handle_ShouldNotPlaceExitOrder_WhenBotIsDisabled()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        await CreateCompletedTrade(bot, entryPrice);
        
        // Disable the bot
        bot.Enabled = false;
        await DbContext.SaveChangesAsync();
        
        var ticker = CreateTestTicker(100.5m, 101m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert
        VerifyNoExitOrdersPlaced(bot);
    }
    
    [Fact]
    public async Task Handle_ShouldCreateExitOrder_WhenPriceIsExactlyAtExitStep()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        await CreateCompletedTrade(bot, entryPrice);
        
        // Create ticker with price exactly at exit step
        var exitPrice = 101m;
        var ticker = CreateTestTicker(101m, 101m); // Both bid and ask at exit price
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup mock for exit order
        SetupConsolidatedExitOrder(bot, exitPrice, bot.EntryQuantity);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert
        VerifyExitOrderPlaced(bot, exitPrice, bot.EntryQuantity);
    }
    
    [Fact]
    public async Task Handle_ShouldCalculateCorrectExitPrices_WhenExitStepIsChanged()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        await CreateCompletedTrade(bot, entryPrice);
        
        // Create ticker with price that doesn't yet reach exit step
        var ticker = CreateTestTicker(100.5m, 100.8m); // Below exit price of 101
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Act - first attempt with original exit step
        await Handle(command, CancellationToken.None);
        
        // Assert - no orders placed as price isn't at exit step yet
        VerifyNoExitOrdersPlaced(bot);
        
        // Now reduce exit step to 0.5
        bot.ExitStep = 0.5m;
        await DbContext.SaveChangesAsync();
        
        // Setup mock for exit order with reduced exit step
        var reducedExitPrice = 100.8m; // With exit step of 0.5, price is now eligible
        SetupConsolidatedExitOrder(bot, reducedExitPrice, bot.EntryQuantity);
        
        // Act - second attempt with reduced exit step
        await Handle(command, CancellationToken.None);
        
        // Assert - exit order placed with new exit step
        VerifyExitOrderPlaced(bot, reducedExitPrice, bot.EntryQuantity);
    }
    
    [Fact]
    public async Task Handle_ShouldNotPlaceExitOrder_WhenEntryOrderIsNotCompleted()
    {
        // Arrange
        var bot = await CreateTestBot(exitStep: 1.0m, isLong: true);
        var entryPrice = 100m;
        
        // Create incomplete entry order
        var incompleteOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong, OrderStatus.New, 0);
        var incompleteTrade = new Trade(incompleteOrder);
        bot.Trades.Add(incompleteTrade);
        
        // Create zero filled entry order
        var zeroFilledOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong, OrderStatus.Filled, 0);
        var zeroFilledTrade = new Trade(zeroFilledOrder);
        bot.Trades.Add(zeroFilledTrade);
        
        // Create completed entry order
        var completedOrder = CreateOrder(bot, entryPrice, bot.EntryQuantity, bot.IsLong, OrderStatus.Filled, bot.EntryQuantity);
        var completedTrade = new Trade(completedOrder);
        bot.Trades.Add(completedTrade);
        
        await DbContext.SaveChangesAsync();
        
        // Create ticker with price that reaches exit step
        var exitPrice = 101m;
        var ticker = CreateTestTicker(100.5m, 101m);
        var command = new PlaceExitOrdersCommand { Ticker = ticker };
        
        // Setup mock for exit order
        SetupConsolidatedExitOrder(bot, exitPrice, completedOrder.QuantityFilled);
        
        // Act
        await Handle(command, CancellationToken.None);
        
        // Assert - exit order placed only for completed trade
        VerifyExitOrderPlaced(bot, exitPrice, completedOrder.QuantityFilled);
        
        // Check incomplete trade has no exit order
        var updatedIncompleteTrade = await DbContext.Trades.FirstOrDefaultAsync(t => t.Id == incompleteTrade.Id);
        Assert.NotNull(updatedIncompleteTrade);
        Assert.Null(updatedIncompleteTrade.ExitOrder);
        
        // Check zero filled trade has no exit order
        var updatedZeroFilledTrade = await DbContext.Trades.FirstOrDefaultAsync(t => t.Id == zeroFilledTrade.Id);
        Assert.NotNull(updatedZeroFilledTrade);
        Assert.Null(updatedZeroFilledTrade.ExitOrder);
        
        // Check completed trade has exit order
        var updatedCompletedTrade = await DbContext.Trades.FirstOrDefaultAsync(t => t.Id == completedTrade.Id);
        Assert.NotNull(updatedCompletedTrade);
        Assert.NotNull(updatedCompletedTrade.ExitOrder);
    }
}
