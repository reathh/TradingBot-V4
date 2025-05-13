using Microsoft.AspNetCore.SignalR;

namespace TradingBot.Services;

/// <summary>
/// SignalR hub for real-time trading updates
/// </summary>
public class TradingHub : Hub<ITradingHubClient>
{
} 