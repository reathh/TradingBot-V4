namespace TradingBot.Tests.Services;

using Data;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.VerifyBotBalance;
using TradingBot.Application.Common;
using TradingBot.Services;

public class BalanceVerificationServiceTests : BaseTest, IDisposable
{
    private BalanceVerificationService _balanceVerificationService;
    private Mock<IMediator> _mockMediator;
    private Mock<ILogger<BalanceVerificationService>> _mockLogger;
    private ServiceProvider _serviceProvider;
    private IServiceScope _serviceScope;

    public BalanceVerificationServiceTests() : base()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<BalanceVerificationService>>();

        // Create a service provider with our DbContext and mocked mediator
        var services = new ServiceCollection();
        services.AddSingleton(DbContext);
        services.AddSingleton(_mockMediator.Object);
        _serviceProvider = services.BuildServiceProvider();
        _serviceScope = _serviceProvider.CreateScope();

        _balanceVerificationService = new BalanceVerificationService(
            _serviceProvider,
            _mockLogger.Object);
    }

    [Fact]
    public async Task VerifyBalancesAsync_SendsSingleCommand()
    {
        // Arrange
        // Create some bots
        for (int i = 0; i < 3; i++)
        {
            var bot = await CreateBot(
                isLong: true,
                maxPrice: 100,
                minPrice: 90,
                entryStep: 1,
                exitStep: 1.5m,
                entryQuantity: 1,
                placeOrdersInAdvance: true
            );
            bot.Enabled = i < 2;
        }
        await DbContext.SaveChangesAsync();

        _mockMediator
            .Setup(m => m.Send(It.IsAny<VerifyBalancesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Act
        await _balanceVerificationService.ExecuteScheduledWork(CancellationToken.None);

        // Assert - verify that a single command was sent
        _mockMediator.Verify(
            m => m.Send(It.IsAny<VerifyBalancesCommand>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    public void Dispose()
    {
        _serviceScope?.Dispose();
        _serviceProvider?.Dispose();
    }
}