using FakeItEasy;
using Microsoft.Extensions.Logging;
using TradingBot.Application.Commands.ExitLosingTrades;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests.Application.Commands.ExitLosingTrades;

using Models;

/// <summary>
/// Base class for all ExitLosingTrades command tests using FakeItEasy
/// </summary>
public abstract class ExitLosingTradesTestBase : BaseTest
{
    protected readonly ExitLosingTradesCommand.ExitLossTradesCommandHandler Handler;
    protected readonly ILogger<ExitLosingTradesCommand.ExitLossTradesCommandHandler> LoggerFake;
    protected readonly ISymbolInfoCache SymbolInfoCacheFake;
    protected readonly TradingNotificationService NotificationServiceStub;
    protected readonly IExchangeApiRepository ExchangeApiRepositoryFake;
    protected readonly IExchangeApi ExchangeApiFake;

    protected ExitLosingTradesTestBase()
    {
        // Set up fake dependencies
        LoggerFake = A.Fake<ILogger<ExitLosingTradesCommand.ExitLossTradesCommandHandler>>();
        SymbolInfoCacheFake = A.Fake<ISymbolInfoCache>();
        NotificationServiceStub = new TestTradingNotificationService();
        ExchangeApiRepositoryFake = A.Fake<IExchangeApiRepository>();
        ExchangeApiFake = A.Fake<IExchangeApi>();
        
        // Configure default behaviors
        var defaultSymbolInfo = new SymbolInfo(0.00001m, 0.00001m, 5);
        A.CallTo(() => SymbolInfoCacheFake.GetAsync(A<string>.Ignored, A<CancellationToken>.Ignored))
            .Returns(defaultSymbolInfo);
        A.CallTo(() => ExchangeApiRepositoryFake.GetExchangeApi(A<Bot>.Ignored))
            .Returns(ExchangeApiFake);
        
        // Create handler with fake dependencies
        Handler = new ExitLosingTradesCommand.ExitLossTradesCommandHandler(
            DbContext,
            ExchangeApiRepositoryFake,
            SymbolInfoCacheFake,
            NotificationServiceStub,
            LoggerFake);
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
    /// Setup exit order for the exchange API
    /// </summary>
    protected void SetupExitOrder(Bot bot, decimal price, decimal quantity, bool isBuy)
    {
        var order = CreateOrder(bot, price, quantity, isBuy);
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.Ignored,
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Returns(order);
    }

    /// <summary>
    /// Setup the exchange API to throw an exception when placing an order
    /// </summary>
    protected void SetupExitOrderFailure(string errorMessage)
    {
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
                A<Bot>.Ignored,
                A<decimal>.Ignored,
                A<decimal>.Ignored,
                A<bool>.Ignored,
                A<OrderType>.Ignored,
                A<CancellationToken>.Ignored))
            .Throws(new Exception(errorMessage));
    }

    /// <summary>
    /// Verify that an exit order was placed with the expected parameters
    /// </summary>
    protected void VerifyExitOrderPlaced(
        Bot bot, 
        decimal expectedPrice,
        decimal expectedQuantity,
        bool expectedIsBuy,
        OrderType expectedOrderType = OrderType.LimitMaker)
    {
        // Verify PlaceOrder was called with exact parameters
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
            A<Bot>.That.Matches(b => b.Id == bot.Id),
            A<decimal>.That.IsEqualTo(expectedPrice),
            A<decimal>.That.IsEqualTo(expectedQuantity),
            A<bool>.That.IsEqualTo(expectedIsBuy),
            A<OrderType>.That.IsEqualTo(expectedOrderType),
            A<CancellationToken>.Ignored))
        .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Verify that no exit order was placed
    /// </summary>
    protected void VerifyNoExitOrderPlaced()
    {
        A.CallTo(() => ExchangeApiFake.PlaceOrder(
            A<Bot>.Ignored,
            A<decimal>.Ignored,
            A<decimal>.Ignored,
            A<bool>.Ignored,
            A<OrderType>.Ignored,
            A<CancellationToken>.Ignored))
        .MustNotHaveHappened();
    }

    /// <summary>
    /// Test notification service that does nothing
    /// </summary>
    private class TestTradingNotificationService() : TradingNotificationService(null, null)
    {
        public override Task NotifyOrderUpdated(string orderId) => Task.CompletedTask;
    }
} 