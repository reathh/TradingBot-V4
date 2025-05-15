using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Models;
using TradingBot.Services;
using Xunit;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

public class ExitLosingTradesBasicTests_FakeItEasy : ExitLosingTradesTestBase
{
    [Fact]
    public async Task Handle_ShouldExitLossTrade_ForLongBot_WhenPriceFallsBelowThreshold_FakeItEasy()
    {
        // Arrange
        var entryPrice = 100m;
        var stopLossPercent = 1.0m; // 1% stop loss
        var stopLossPrice = entryPrice * (1 - stopLossPercent / 100m); // 99.0
        var currentBid = stopLossPrice - 0.1m; // 98.9 - just below stop loss
        var currentAsk = currentBid + 0.2m; // 99.1 - ask price for the ticker

        // Create bot and trade
        var bot = await CreateStopLossBot(isLong: true, stopLossPercent: stopLossPercent);
        var trade = await CreateFilledTrade(bot, entryPrice, bot.EntryQuantity);
        
        // For long positions exiting, we should use ask price for limit maker orders
        var exitPrice = currentAsk;
        var exitOrder = CreateOrder(bot, exitPrice, bot.EntryQuantity, false);

        // Configure FakeItEasy with strict expectations
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(exitPrice),
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
        
        // Verify exit order was placed with exact parameters
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.That.Matches(b => b.Id == bot.Id),
                A<decimal>.That.IsEqualTo(exitPrice),
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