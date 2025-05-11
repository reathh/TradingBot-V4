using Microsoft.EntityFrameworkCore;
using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Tests for basic entry order placement functionality
/// </summary>
public class PlaceEntryOrdersBasicTests : BaseTest
{
    [Fact]
    public async Task Handle_ShouldPlaceOrder_WhenBotIsEnabledAndNoOpenTrades()
    {
        // Arrange
        var bot = await CreateBot();
        var ticker = CreateTicker(100, 101);
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        var expectedOrder = CreateOrder(bot, bot.IsLong ? ticker.Bid : ticker.Ask, bot.EntryQuantity, bot.IsLong);
        ExchangeApiMock.Setup(x => x.PlaceOrder(
                It.IsAny<Bot>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.Is<Bot>(b => b.Id == bot.Id),
            It.Is<decimal>(p => p == (bot.IsLong ? ticker.Bid : ticker.Ask)),
            It.Is<decimal>(q => q == bot.EntryQuantity),
            It.Is<bool>(b => b == bot.IsLong),
            It.IsAny<CancellationToken>()), Times.Once);

        var savedTrade = await DbContext.Trades.FirstOrDefaultAsync();
        Assert.NotNull(savedTrade);
        Assert.Equal(expectedOrder.Price, savedTrade.EntryOrder.Price);
        Assert.Equal(expectedOrder.Quantity, savedTrade.EntryOrder.Quantity);
    }

    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenBotIsDisabled()
    {
        // Arrange
        var bot = await CreateBot();
        bot.Enabled = false;
        await DbContext.SaveChangesAsync();

        var ticker = CreateTicker(100, 101);
        var command = new PlaceEntryOrdersCommand { Ticker = ticker };

        // Act
        await Handler.Handle(command, CancellationToken.None);

        // Assert
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
} 