using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Tests to verify that additional entry orders are NOT placed when the market moves in the opposite direction
/// of the averaging-down / up logic (i.e. price rises for a long bot or falls for a short bot).
/// </summary>
public class PlaceEntryOrdersDirectionalTests : BaseTest
{
    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceRises_ForLongBot()
    {
        // Arrange – create a long bot with one existing open trade at 100
        var bot = await CreateBot(entryQuantity: 1, entryStep: 1m, isLong: true);

        // Add an existing open trade at price 100
        var existingOrder = CreateOrder(bot, 100m, bot.EntryQuantity, bot.IsLong, OrderStatus.New);
        var existingTrade = new Trade(existingOrder);
        bot.Trades.Add(existingTrade);
        await DbContext.SaveChangesAsync();

        // Current ticker where price has moved UP above the first trade price
        var ticker = CreateTicker(105m, 106m); // Bid 105 (>100)
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert – no new orders should be placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceFalls_ForShortBot()
    {
        // Arrange – create a short bot with one existing open trade at 101
        var bot = await CreateBot(entryQuantity: 1, entryStep: 1m, isLong: false);

        // Add an existing open trade at price 101
        var existingOrder = CreateOrder(bot, 101m, bot.EntryQuantity, bot.IsLong, OrderStatus.New);
        var existingTrade = new Trade(existingOrder);
        bot.Trades.Add(existingTrade);
        await DbContext.SaveChangesAsync();

        // Current ticker where price has moved DOWN below the first trade price
        var ticker = CreateTicker(95m, 96m); // Ask 96 (<101)
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert – no new orders should be placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<OrderType>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
} 