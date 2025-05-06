using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.UpdateBotOrder;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests;

public class OrderUpdateServiceTests : BaseTest, IDisposable
{
    private readonly Mock<IExchangeApiRepository> _exchangeApiRepositoryMock;
    private readonly Mock<IExchangeApi> _exchangeApiMock;
    private readonly Mock<ILogger<OrderUpdateService>> _loggerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ServiceProvider _serviceProvider;
    private OrderUpdateService _service;

    public OrderUpdateServiceTests()
    {
        _exchangeApiMock = new Mock<IExchangeApi>();
        _exchangeApiRepositoryMock = new Mock<IExchangeApiRepository>();
        _loggerMock = new Mock<ILogger<OrderUpdateService>>();
        _mediatorMock = new Mock<IMediator>();

        // Configure the repository mock to return the API mock
        _exchangeApiRepositoryMock
            .Setup(x => x.GetExchangeApi(It.IsAny<Bot>()))
            .Returns(_exchangeApiMock.Object);

        // Configure service provider with required services
        var services = new ServiceCollection();

        services.AddSingleton(DbContext);
        services.AddSingleton(_mediatorMock.Object);

        _serviceProvider = services.BuildServiceProvider();

        // Create the service
        _service = new OrderUpdateService(
            _serviceProvider,
            _exchangeApiRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSubscribeToOrderUpdates_ForAllEnabledBots()
    {
        // Arrange
        var enabledBot1 = await CreateBot(entryQuantity: 1);
        var enabledBot2 = await CreateBot(entryQuantity: 2);
        var disabledBot = await CreateBot(entryQuantity: 3);
        disabledBot.Enabled = false;
        await DbContext.SaveChangesAsync();

        // Setup exchange API mock to track subscriptions
        var subscriptions = new List<Bot>();
        _exchangeApiMock
            .Setup(x => x.SubscribeToOrderUpdates(
                It.IsAny<Func<OrderUpdate, Task>>(),
                It.IsAny<Bot>(),
                It.IsAny<CancellationToken>()))
            .Callback<Func<OrderUpdate, Task>, Bot, CancellationToken>((_, bot, __) => subscriptions.Add(bot))
            .Returns(Task.CompletedTask);

        // Use a token that will cancel after a short time
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act - start the service
        try
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(200); // Give it time to process
        }
        catch (OperationCanceledException)
        {
            // Expected when token is canceled
        }

        // Assert - verify subscriptions were created for enabled bots only
        Assert.Equal(2, subscriptions.Count);
        Assert.Contains(subscriptions, b => b.Id == enabledBot1.Id);
        Assert.Contains(subscriptions, b => b.Id == enabledBot2.Id);
        Assert.DoesNotContain(subscriptions, b => b.Id == disabledBot.Id);

        // Verify repository was called for each enabled bot
        _exchangeApiRepositoryMock.Verify(
            r => r.GetExchangeApi(It.Is<Bot>(b => b.Id == enabledBot1.Id || b.Id == enabledBot2.Id)),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessOrderUpdate_ShouldSendUpdateBotOrderCommand_WhenOrderUpdateReceived()
    {
        // Arrange
        var orderUpdate = new OrderUpdate(
            Id: "test-id",
            Symbol: "BTCUSDT",
            Price: 100m,
            Quantity: 1m,
            QuantityFilled: 0.5m,
            AverageFillPrice: 100.2m,
            IsBuy: true,
            Canceled: false,
            Closed: false
        );

        // Setup mediator to respond to UpdateBotOrderCommand
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateBotOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup exchange API to capture the callback
        Func<OrderUpdate, Task> capturedCallback = null;
        _exchangeApiMock
            .Setup(x => x.SubscribeToOrderUpdates(
                It.IsAny<Func<OrderUpdate, Task>>(),
                It.IsAny<Bot>(),
                It.IsAny<CancellationToken>()))
            .Callback<Func<OrderUpdate, Task>, Bot, CancellationToken>((callback, _, __) => capturedCallback = callback)
            .Returns(Task.CompletedTask);

        // Create an enabled bot so the service subscribes
        var bot = await CreateBot();
        await DbContext.SaveChangesAsync();

        // Start the service
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(50); // Give it time to process
        }
        catch (OperationCanceledException)
        {
            // Expected when token is canceled
        }

        // Act - invoke the captured callback with an order update
        Assert.NotNull(capturedCallback); // Ensure callback was captured
        await capturedCallback(orderUpdate);

        // Assert - verify command was sent to mediator
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<UpdateBotOrderCommand>(cmd => cmd.OrderUpdate == orderUpdate),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}