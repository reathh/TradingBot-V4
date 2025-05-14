using Microsoft.AspNetCore.SignalR;

namespace TradingBot.Services;

/// <summary>
/// Service for sending real-time notifications about trading events
/// </summary>
public class TradingNotificationService(IHubContext<TradingHub, ITradingHubClient> hubContext, ILogger<TradingNotificationService> logger)
{
    /// <summary>
    /// Notifies all clients that an order has been updated
    /// </summary>
    public virtual async Task NotifyOrderUpdated(string orderId)
    {
        logger.LogDebug("Notifying clients about order update: {OrderId}", orderId);
        await hubContext.Clients.All.OnOrderUpdated(orderId);
    }
} 