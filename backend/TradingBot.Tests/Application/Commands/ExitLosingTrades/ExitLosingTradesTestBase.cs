using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

using Models;

/// <summary>
/// Base class for all ExitLosingTrades command tests
/// </summary>
public abstract class ExitLosingTradesTestBase : BaseTest
{
    protected readonly ExitLosingTradesCommand.ExitLossTradesCommandHandler Handler;
    protected readonly Mock<ILogger<ExitLosingTradesCommand.ExitLossTradesCommandHandler>> LoggerMock;
    protected readonly Mock<ISymbolInfoCache> SymbolInfoCacheMock;
    protected readonly TradingNotificationService NotificationServiceStub;

    protected ExitLosingTradesTestBase()
    {
        LoggerMock = new Mock<ILogger<ExitLosingTradesCommand.ExitLossTradesCommandHandler>>();
        SymbolInfoCacheMock = new Mock<ISymbolInfoCache>();
        NotificationServiceStub = new TestTradingNotificationService();
        var defaultSymbolInfo = new SymbolInfo(0.00001m, 0.00001m, 5);
        SymbolInfoCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultSymbolInfo);
        Handler = new ExitLosingTradesCommand.ExitLossTradesCommandHandler(
            DbContext,
            ExchangeApiRepositoryMock.Object,
            SymbolInfoCacheMock.Object,
            NotificationServiceStub,
            LoggerMock.Object);
    }

    /// <summary>
    /// Helper method to handle exit loss trades commands
    /// </summary>
    protected Task<Result> Handle(ExitLosingTradesCommand command, CancellationToken cancellationToken = default)
    {
        return Handler.Handle(command, cancellationToken);
    }

    /// <summary>
    /// Creates a bot with stop loss enabled and the specified parameters
    /// </summary>
    protected async Task<Bot> CreateStopLossBot(
        bool isLong = true,
        decimal stopLossPercent = 1.0m,
        decimal entryQuantity = 0.01m)
    {
        var bot = await CreateBot(isLong: isLong, entryQuantity: entryQuantity);
        bot.StopLossEnabled = true;
        bot.StopLossPercent = stopLossPercent;
        await DbContext.SaveChangesAsync();
        return bot;
    }

    /// <summary>
    /// Creates a trade with filled entry order at the specified price
    /// </summary>
    protected async Task<Trade> CreateFilledTrade(Bot bot, decimal entryPrice, decimal quantity, decimal? fee = null)
    {
        var entryOrder = CreateOrder(
            bot, 
            entryPrice, 
            quantity, 
            bot.IsLong, 
            OrderStatus.Filled, 
            quantity,
            fee);
            
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();
        return trade;
    }

    /// <summary>
    /// Setup exit order mock for the exchange API
    /// </summary>
    protected void SetupExitOrder(Bot bot, decimal price, decimal quantity)
    {
        var order = CreateOrder(bot, price, quantity, !bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
    }

    /// <summary>
    /// Setup the exchange API to throw an exception when placing an order
    /// </summary>
    protected void SetupExitOrderFailure(string errorMessage)
    {
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<OrderType>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errorMessage));
    }

    /// <summary>
    /// Verify that an exit order was placed with the expected parameters
    /// </summary>
    protected void VerifyExitOrderPlaced(
        Bot bot, 
        Times times, 
        decimal? expectedPrice = null,
        decimal? expectedQuantity = null,
        bool? expectedIsBuy = null)
    {
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            expectedPrice.HasValue ? It.Is<decimal>(p => p == expectedPrice.Value) : It.IsAny<decimal>(),
            expectedQuantity.HasValue ? It.Is<decimal>(q => q == expectedQuantity.Value) : It.IsAny<decimal>(),
            expectedIsBuy.HasValue ? It.Is<bool>(b => b == expectedIsBuy.Value) : It.IsAny<bool>(),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()), times);
    }

    /// <summary>
    /// Test notification service that does nothing
    /// </summary>
    private class TestTradingNotificationService() : TradingNotificationService(null, null)
    {
        public override Task NotifyOrderUpdated(string orderId) => Task.CompletedTask;
    }
} 