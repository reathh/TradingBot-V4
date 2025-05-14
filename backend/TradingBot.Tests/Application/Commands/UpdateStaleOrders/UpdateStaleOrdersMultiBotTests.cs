using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.UpdateStaleOrders;
using TradingBot.Data;
using TradingBot.Services;
using Xunit;

namespace TradingBot.Tests.Application.Commands.UpdateStaleOrders;

public class UpdateStaleOrdersMultiBotTests : BaseTest
{
    [Fact]
    public async Task Handle_ShouldUpdateStaleOrders_ForMultipleBots()
    {
        // Arrange
        var bot1 = await CreateBot();
        var bot2 = await CreateBot();
        var staleOrder1 = CreateOrder(bot1, 100m, 1m, true);
        var staleOrder2 = CreateOrder(bot2, 200m, 2m, false);
        staleOrder1.LastUpdated = DateTime.UtcNow.AddMinutes(-15);
        staleOrder2.LastUpdated = DateTime.UtcNow.AddMinutes(-20);
        DbContext.Orders.AddRange(staleOrder1, staleOrder2);
        var trade1 = new Trade(staleOrder1);
        var trade2 = new Trade(staleOrder2);
        bot1.Trades.Add(trade1);
        bot2.Trades.Add(trade2);
        DbContext.Trades.AddRange(trade1, trade2);
        await DbContext.SaveChangesAsync();

        ExchangeApiMock.Setup(x => x.GetOrderStatus(staleOrder1.Id, bot1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderUpdate(
                Id: staleOrder1.Id,
                Symbol: staleOrder1.Symbol,
                Price: staleOrder1.Price,
                Quantity: staleOrder1.Quantity,
                QuantityFilled: staleOrder1.Quantity,
                AverageFillPrice: 101m,
                IsBuy: staleOrder1.IsBuy,
                Status: OrderStatus.Filled,
                Fee: null));
        ExchangeApiMock.Setup(x => x.GetOrderStatus(staleOrder2.Id, bot2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderUpdate(
                Id: staleOrder2.Id,
                Symbol: staleOrder2.Symbol,
                Price: staleOrder2.Price,
                Quantity: staleOrder2.Quantity,
                QuantityFilled: staleOrder2.Quantity,
                AverageFillPrice: 202m,
                IsBuy: staleOrder2.IsBuy,
                Status: OrderStatus.Filled,
                Fee: null));

        var timeProviderMock = new Mock<TimeProvider>();
        timeProviderMock.Setup(x => x.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
        var handler = new UpdateStaleOrdersCommandHandler(
            DbContext,
            ExchangeApiRepositoryMock.Object,
            timeProviderMock.Object,
            NotificationServiceStub,
            Mock.Of<ILogger<UpdateStaleOrdersCommandHandler>>());

        var command = new UpdateStaleOrdersCommand { StaleThreshold = TimeSpan.FromMinutes(10) };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Data); // Both orders should be updated
    }
} 