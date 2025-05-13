namespace TradingBot.Services;

/// <summary>
/// Interface for SignalR hub client methods
/// </summary>
public interface ITradingHubClient
{
    /// <summary>
    /// Notifies clients when an order is updated
    /// </summary>
    Task OnOrderUpdated(string orderId);
} 