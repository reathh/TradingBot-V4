using System.Threading.Tasks;
using TradingBot.Services;

namespace TradingBot.Tests.Helpers;

public class TestTradingNotificationService : TradingNotificationService
{
    public TestTradingNotificationService() : base(null, null) { }
    public new Task NotifyOrderUpdated(string orderId) => Task.CompletedTask;
} 