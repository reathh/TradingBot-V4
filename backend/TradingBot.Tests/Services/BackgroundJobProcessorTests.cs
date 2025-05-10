using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Common;
using TradingBot.Services;

namespace TradingBot.Tests;

// Test command classes implementing both IRequest and IRequest<Result>
public class TestCommand : IRequest, IRequest<Result>
{
    public string Name { get; set; } = "";
}

public class TestResultCommand : IRequest, IRequest<Result>
{
    public string Name { get; set; } = "";
}

public class BackgroundJobProcessorTests : IDisposable
{
    private readonly Mock<ILogger<BackgroundJobProcessor>> _loggerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ServiceProvider _serviceProvider;
    private readonly BackgroundJobProcessor _processor;

    public BackgroundJobProcessorTests()
    {
        _loggerMock = new Mock<ILogger<BackgroundJobProcessor>>();
        _mediatorMock = new Mock<IMediator>();

        // Configure service provider with required services
        var services = new ServiceCollection();
        services.AddSingleton(_mediatorMock.Object);
        _serviceProvider = services.BuildServiceProvider();

        // Create the processor
        _processor = new BackgroundJobProcessor(
            _serviceProvider,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task StartAsync_ShouldInitializeProcessor()
    {
        // Act
        await _processor.StartAsync(CancellationToken.None);
        
        // Assert - Logs should show processor started
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Background job processor started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        // Cleanup
        await _processor.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Enqueue_ShouldEnqueueJob()
    {
        // Arrange
        await _processor.StartAsync(CancellationToken.None);
        var command = new TestCommand { Name = "TestCommand" };
        
        // Setup mediator to return success
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result.Success));

        // Act
        _processor.Enqueue(command);
        
        // Assert - Logs should show job was enqueued
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Enqueued job")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        // Cleanup
        await _processor.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_ShouldStopProcessor()
    {
        // Arrange
        await _processor.StartAsync(CancellationToken.None);
        
        // Act
        await _processor.StopAsync(CancellationToken.None);
        
        // Assert - Logs should show processor stopping
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Background job processor stopping")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}