using Moq;
using TradingBot.Application.Commands.PlaceEntryOrders;
using TradingBot.Data;

namespace TradingBot.Tests.Application.Commands.PlaceEntryOrders;

/// <summary>
/// Tests for price boundary conditions in entry order placement
/// </summary>
public class PlaceEntryOrdersPriceBoundaryTests : BaseTest
{
    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceIsAboveMaxPrice()
    {
        // Arrange
        var bot = await CreateBot(maxPrice: 100);
        var ticker = CreateTicker(101, 102);
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

    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceIsBelowMinPrice()
    {
        // Arrange
        var bot = await CreateBot(minPrice: 100);
        var ticker = CreateTicker(98, 99);
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

    [Fact]
    public async Task Handle_ShouldNotPlaceOrder_WhenPriceIsBelowMinPrice_ShortBot()
    {
        // Arrange
        var bot = await CreateBot(minPrice: 100, isLong: false);
        var ticker = CreateTicker(98, 99);  // Both bid and ask are below MinPrice
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

    [Fact]
    public async Task Handle_ShouldPlaceOrder_WhenPriceIsExactlyAtMinPrice()
    {
        // Arrange
        var minPrice = 100m;
        var bot = await CreateBot(minPrice: minPrice);
        var ticker = CreateTicker(minPrice - 1, minPrice); // Ask is exactly at MinPrice
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

        // Assert - Order should be placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPlaceOrder_WhenPriceIsExactlyAtMaxPrice()
    {
        // Arrange
        var maxPrice = 100m;
        var bot = await CreateBot(maxPrice: maxPrice);
        var ticker = CreateTicker(maxPrice, maxPrice + 1); // Bid is exactly at MaxPrice
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

        // Assert - Order should be placed
        ExchangeApiMock.Verify(x => x.PlaceOrder(
            It.IsAny<Bot>(),
            It.IsAny<decimal>(),
            It.IsAny<decimal>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
} 