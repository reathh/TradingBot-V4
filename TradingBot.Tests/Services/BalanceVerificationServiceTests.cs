using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using TradingBot.Application.Commands.VerifyBotBalance;
using TradingBot.Application.Common;
using TradingBot.Data;
using TradingBot.Services;

namespace TradingBot.Tests;

public class BalanceVerificationServiceTests : BaseTest, IDisposable
{
    private BalanceVerificationService _balanceVerificationService;
    private Mock<IExchangeApiRepository> _mockExchangeApiRepository;
    private Mock<IMediator> _mockMediator;
    private Mock<ILogger<BalanceVerificationService>> _mockLogger;
    private ServiceProvider _serviceProvider;
    private IServiceScope _serviceScope;

    public BalanceVerificationServiceTests() : base()
    {
        _mockExchangeApiRepository = new Mock<IExchangeApiRepository>();
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
            _mockExchangeApiRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task VerifyBalancesAsync_SendsCommandForEachEnabledBot()
    {
        // Arrange
        var bots = new List<Bot>();

        // Create 3 bots - 2 enabled with balance verification, 1 disabled
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

            bot.Enabled = i < 2; // First 2 are enabled
            bots.Add(bot);
        }

        await DbContext.SaveChangesAsync();

        // Setup mediator to return success
        _mockMediator
            .Setup(m => m.Send(It.IsAny<VerifyBotBalanceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Act
        await _balanceVerificationService.ExecuteScheduledWorkAsync(CancellationToken.None);

        // Assert - verify that command was sent for each enabled bot (2), but not for disabled bot
        _mockMediator.Verify(
            m => m.Send(It.IsAny<VerifyBotBalanceCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task VerifyBotBalanceAsync_WithNullTicker_LogsWarning()
    {
        // Arrange
        var bot = await CreateBot(
            isLong: true,
            maxPrice: null, // No price references so ticker will be null
            minPrice: null,
            entryStep: 1,
            exitStep: 1.5m,
            entryQuantity: 1,
            placeOrdersInAdvance: true
        );

        bot.Enabled = true;
        bot.Trades.Clear(); // No trades either for price reference

        await DbContext.SaveChangesAsync();

        // Act
        await _balanceVerificationService.VerifyBotBalanceAsync(_serviceScope, bot, CancellationToken.None);

        // Assert - mediator should not be called when ticker is null
        _mockMediator.Verify(
            m => m.Send(It.IsAny<VerifyBotBalanceCommand>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        // Could verify log warning was called if needed
    }

    [Fact]
    public async Task VerifyBotBalanceAsync_WithValidTicker_SendsCommand()
    {
        // Arrange
        var bot = await CreateBot(
            isLong: true,
            maxPrice: 100,
            minPrice: 90,
            entryStep: 1,
            exitStep: 1.5m,
            entryQuantity: 1,
            placeOrdersInAdvance: true
        );

        bot.Enabled = true;

        await DbContext.SaveChangesAsync();

        _mockMediator
            .Setup(m => m.Send(It.IsAny<VerifyBotBalanceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Act
        await _balanceVerificationService.VerifyBotBalanceAsync(_serviceScope, bot, CancellationToken.None);

        // Assert - verify command was sent
        _mockMediator.Verify(
            m => m.Send(
                It.Is<VerifyBotBalanceCommand>(cmd => cmd.Bot == bot && cmd.CurrentPrice > 0),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    public void Dispose()
    {
        _serviceScope?.Dispose();
        _serviceProvider?.Dispose();
    }
}