using Microsoft.AspNetCore.SignalR;

namespace TradingBot.Services;

/// <summary>
/// Service for sending real-time notifications about trading events
/// </summary>
public class TradingNotificationService
{
    private readonly IHubContext<TradingHub, ITradingHubClient> _hubContext;
    private readonly ILogger<TradingNotificationService> _logger;

    public TradingNotificationService(
        IHubContext<TradingHub, ITradingHubClient> hubContext,
        ILogger<TradingNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Notifies all clients that an order has been updated
    /// </summary>
    public async Task NotifyOrderUpdated(string orderId)
    {
        _logger.LogDebug("Notifying clients about order update: {OrderId}", orderId);
        await _hubContext.Clients.All.OnOrderUpdated(orderId);
    }
} 