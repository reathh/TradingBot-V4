namespace TradingBot.Tests.Application.Commands.VerifyBotBalance;

using Data;
using Microsoft.Extensions.Logging;
using Moq;
using TradingBot.Application.Commands.VerifyBotBalance;
using TradingBot.Services;

public class VerifyBotBalanceCommandTests : BaseTest
{
    private VerifyBotBalanceCommand.VerifyBotBalanceCommandHandler _handler;
    private Mock<IExchangeApiRepository> _mockExchangeApiRepository;
    private Mock<IExchangeApi> _mockExchangeApi;
    private Mock<ILogger<VerifyBotBalanceCommand.VerifyBotBalanceCommandHandler>> _mockLogger;

    public VerifyBotBalanceCommandTests() : base()
    {
        _mockExchangeApi = new Mock<IExchangeApi>();
        _mockExchangeApiRepository = new Mock<IExchangeApiRepository>();
        _mockLogger = new Mock<ILogger<VerifyBotBalanceCommand.VerifyBotBalanceCommandHandler>>();

        _mockExchangeApiRepository
            .Setup(repo => repo.GetExchangeApi(It.IsAny<Bot>()))
            .Returns(_mockExchangeApi.Object);

        _handler = new VerifyBotBalanceCommand.VerifyBotBalanceCommandHandler(
            DbContext,
            _mockExchangeApiRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LongBot_BalanceVerification_CalculatesCorrectly()
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

        // Add starting base amount
        bot.StartingBaseAmount = 10;

        // Create scenario where price has moved from 100 to 95
        // We should have bought 5 units at prices 99, 98, 97, 96, 95

        // Create entry orders
        for (decimal price = 99; price >= 95; price--)
        {
            var order = CreateOrder(bot, price, 1, true);
            order.QuantityFilled = 1; // Fully filled

            var trade = new Trade(order);
            DbContext.Add(trade);
            bot.Trades.Add(trade);
        }

        // Add one exit order that's partially filled (0.3 units sold)
        var exitOrder = CreateOrder(bot, 100.5m, 1, false);
        exitOrder.QuantityFilled = 0.3m;

        var firstTrade = bot.Trades.First();
        firstTrade.ExitOrder = exitOrder;

        await DbContext.SaveChangesAsync();

        // Expected balance: 5 + 10 starting - 0.3 sold = 14.7
        decimal expectedBalance = 14.7m;

        // Set up the mock API to return a correct balance
        _mockExchangeApi
            .Setup(api => api.GetBalance(It.IsAny<string>(), bot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBalance);

        // Create command
        var command = new VerifyBotBalanceCommand
        {
            Bot = bot
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ShortBot_BalanceVerification_CalculatesCorrectly()
    {
        // Arrange
        var bot = await CreateBot(
            isLong: false,
            maxPrice: 110,
            minPrice: 100,
            entryStep: 1,
            exitStep: 1.5m,
            entryQuantity: 1,
            placeOrdersInAdvance: true
        );

        // Add starting base amount
        bot.StartingBaseAmount = 20;

        // Create scenario where price has moved from 100 to 105
        // We should have sold 5 units at prices 101, 102, 103, 104, 105

        // Create entry (sell) orders
        for (decimal price = 101; price <= 105; price++)
        {
            var order = CreateOrder(bot, price, 1, false);
            order.QuantityFilled = 1; // Fully filled

            var trade = new Trade(order);
            DbContext.Add(trade);
            bot.Trades.Add(trade);
        }

        // Add one exit (buy) order that's partially filled (0.5 units bought back)
        var exitOrder = CreateOrder(bot, 99.5m, 1, true);
        exitOrder.QuantityFilled = 0.5m;

        var firstTrade = bot.Trades.First();
        firstTrade.ExitOrder = exitOrder;

        await DbContext.SaveChangesAsync();

        // Expected balance: 20 starting - 5 sold + 0.5 bought back = 15.5
        decimal expectedBalance = 15.5m;

        // Set up the mock API to return a correct balance
        _mockExchangeApi
            .Setup(api => api.GetBalance(It.IsAny<string>(), bot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBalance);

        // Create command
        var command = new VerifyBotBalanceCommand
        {
            Bot = bot
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task BalanceVerification_RetryMechanism_WorksCorrectly()
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
        
        // Make sure we can identify the expected balance
        bot.StartingBaseAmount = 5.0m;

        // Set the delay between checks to zero to make tests faster
        var delayField = typeof(VerifyBotBalanceCommand.VerifyBotBalanceCommandHandler)
            .GetField("_delayBetweenChecks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        delayField?.SetValue(_handler, TimeSpan.Zero);

        // Hijack the handler class to use our mock expected balance
        var expectedBalanceValue = bot.StartingBaseAmount;
        
        // Mock the mechanism that runs the retry logic - first return wrong values, then return matching value
        var callCount = 0;
        _mockExchangeApi
            .Setup(api => api.GetBalance(It.IsAny<string>(), bot, It.IsAny<CancellationToken>()))
            .Returns(() => {
                callCount++;
                // First two calls return wrong values, third call returns correct expected value
                switch (callCount)
                {
                    case 1: return Task.FromResult(1.0m); // First call incorrect
                    case 2: return Task.FromResult(3.0m); // Second call incorrect
                    default: return Task.FromResult(expectedBalanceValue); // Third call matches expected
                }
            });
        
        // Create command
        var command = new VerifyBotBalanceCommand
        {
            Bot = bot
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        
        // Verify the mock was called the expected number of times
        _mockExchangeApi.Verify(
            api => api.GetBalance(It.IsAny<string>(), It.IsAny<Bot>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(3));
    }

    [Fact]
    public async Task BalanceVerification_WithWrongBalance_DisablesBot()
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

        bot.StartingBaseAmount = 10;
        bot.Enabled = true;

        // Set the delay between checks to zero to make tests faster
        var delayField = typeof(VerifyBotBalanceCommand.VerifyBotBalanceCommandHandler)
            .GetField("_delayBetweenChecks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        delayField?.SetValue(_handler, TimeSpan.Zero);

        // Expected balance calculation will be around 10 (starting amount)
        // But API returns a significantly different amount
        _mockExchangeApi
            .Setup(api => api.GetBalance(It.IsAny<string>(), bot, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5.0m); // Significantly different from expected 10.0

        // Create command
        var command = new VerifyBotBalanceCommand
        {
            Bot = bot
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(bot.Enabled); // Bot should be disabled
    }
}