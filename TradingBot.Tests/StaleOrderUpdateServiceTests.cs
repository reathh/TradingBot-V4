using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.UpdateStaleOrders;
using TradingBot.Application.Common;
using TradingBot.Services;

namespace TradingBot.Tests;

public class StaleOrderUpdateServiceTests : BaseTest, IDisposable
{
    private readonly Mock<ILogger<StaleOrderUpdateService>> _loggerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ServiceProvider _serviceProvider;
    private readonly StaleOrderUpdateService _service;

    public StaleOrderUpdateServiceTests()
    {
        _loggerMock = new Mock<ILogger<StaleOrderUpdateService>>();
        _mediatorMock = new Mock<IMediator>();

        // Configure service provider with required services
        var services = new ServiceCollection();
        services.AddSingleton(_mediatorMock.Object);
        _serviceProvider = services.BuildServiceProvider();

        // Create the service
        _service = new StaleOrderUpdateService(
            _serviceProvider,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSendUpdateStaleOrdersCommand_Periodically()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateStaleOrdersCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.SuccessWith(5)); // Mock 5 orders updated

        // Use a token that will cancel after a short time
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act - start the service (should run at least once)
        try
        {
            await _service.StartAsync(cts.Token);
            await Task.Delay(200); // Give it time to process
        }
        catch (OperationCanceledException)
        {
            // Expected when token is canceled
        }

        // Assert - verify command was sent to mediator
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<UpdateStaleOrdersCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateStaleOrdersAsync_ShouldHandleFailures_Gracefully()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateStaleOrdersCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure(["Test error"]));

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

        // Assert - verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o != null && o.ToString()!.Contains("Failed to update stale orders")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateStaleOrdersAsync_ShouldHandleExceptions_Gracefully()
    {
        // Arrange
        var expectedException = new Exception("Test exception");
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateStaleOrdersCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

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

        // Assert - verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o != null && o.ToString()!.Contains("Error in")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}