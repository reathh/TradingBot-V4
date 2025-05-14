using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.PlaceExitOrders;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.PlaceExitOrders;

using Models;

/// <summary>
/// Base class for all PlaceExitOrders command tests
/// </summary>
public abstract class PlaceExitOrdersTestBase : BaseTest
{
    protected new readonly PlaceExitOrdersCommand.PlaceExitOrdersCommandHandler Handler;
    protected new readonly Mock<ILogger<PlaceExitOrdersCommand.PlaceExitOrdersCommandHandler>> LoggerMock;
    protected readonly Mock<ISymbolInfoCache> SymbolInfoCacheMock;
    protected readonly TradingNotificationService NotificationServiceStub;

    protected PlaceExitOrdersTestBase()
    {
        LoggerMock = new Mock<ILogger<PlaceExitOrdersCommand.PlaceExitOrdersCommandHandler>>();
        SymbolInfoCacheMock = new Mock<ISymbolInfoCache>();
        NotificationServiceStub = new TestTradingNotificationService();
        var defaultSymbolInfo = new SymbolInfo(0.00001m, 0.00001m, 5);
        SymbolInfoCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultSymbolInfo);
        Handler = new PlaceExitOrdersCommand.PlaceExitOrdersCommandHandler(
            DbContext,
            ExchangeApiRepositoryMock.Object,
            SymbolInfoCacheMock.Object,
            NotificationServiceStub,
            LoggerMock.Object);
    }

    /// <summary>
    /// Helper method to handle exit orders commands
    /// </summary>
    protected Task Handle(PlaceExitOrdersCommand command, CancellationToken cancellationToken = default)
    {
        return Handler.Handle(command, cancellationToken);
    }

    /// <summary>
    /// Creates a ticker with the specified bid and ask prices
    /// </summary>
    protected TickerDto CreateTestTicker(decimal bid, decimal ask)
    {
        var lastPrice = (bid + ask) / 2;
        var now = DateTime.Now;
        return new TickerDto(
            "BTC/USDT", now, bid, ask, lastPrice, lastPrice, 
            lastPrice * 1.01m, lastPrice * 0.99m, 1000m, 
            1000m * lastPrice, lastPrice, 0m, 0m, 
            100, now.AddDays(-1), now);
    }

    /// <summary>
    /// Creates a bot with the specified parameters and saves it to the database
    /// </summary>
    protected async Task<Bot> CreateTestBot(
        decimal exitStep = 1.0m,
        bool isLong = true,
        decimal entryQuantity = 1.0m,
        bool placeOrdersInAdvance = false,
        int ordersInAdvance = 0)
    {
        var bot = await CreateBot(
            exitStep: exitStep,
            isLong: isLong,
            entryQuantity: entryQuantity,
            placeOrdersInAdvance: placeOrdersInAdvance,
            ordersInAdvance: ordersInAdvance);
            
        await DbContext.SaveChangesAsync();
        return bot;
    }

    /// <summary>
    /// Creates a trade with a completed entry order
    /// </summary>
    protected async Task<Trade> CreateCompletedTrade(Bot bot, decimal entryPrice)
    {
        var entryOrder = CreateOrder(
            bot, 
            entryPrice, 
            bot.EntryQuantity, 
            bot.IsLong, 
            OrderStatus.Filled, 
            bot.EntryQuantity);
            
        var trade = new Trade(entryOrder);
        bot.Trades.Add(trade);
        await DbContext.SaveChangesAsync();
        return trade;
    }

    /// <summary>
    /// Sets up a consolidated exit order for the mocked exchange API
    /// </summary>
    protected void SetupConsolidatedExitOrder(Bot bot, decimal price, decimal quantity)
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
    /// Sets up a sequence of exit orders for the mocked exchange API
    /// </summary>
    protected void SetupExitOrderSequence(Bot bot, params (decimal price, decimal quantity)[] orders)
    {
        var sequence = ExchangeApiMock.SetupSequence(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()));

        foreach (var (price, quantity) in orders)
        {
            var order = CreateOrder(bot, price, quantity, !bot.IsLong);
            sequence = sequence.ReturnsAsync(order);
        }
    }

    /// <summary>
    /// Verifies that an exit order was placed with the exact parameters
    /// </summary>
    protected void VerifyExitOrderPlaced(
        Bot bot, 
        decimal expectedPrice, 
        decimal expectedQuantity, 
        Times? times = null)
    {
        var timesValue = times ?? Times.Once();
        
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == expectedPrice),
            It.Is<decimal>(q => q == expectedQuantity),
            It.Is<bool>(b => b != bot.IsLong),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()), timesValue);
    }

    /// <summary>
    /// Verifies that no exit orders were placed for the specified bot
    /// </summary>
    protected void VerifyNoExitOrdersPlaced(Bot bot)
    {
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private class TestTradingNotificationService() : TradingNotificationService(null, null)
    {
        public Task NotifyOrderUpdated(string orderId) => Task.CompletedTask;
    }
}
