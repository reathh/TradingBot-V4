namespace TradingBot.Data;

/// <summary>
/// Represents an order update from the exchange
/// </summary>
public record OrderUpdate(
    string Id,
    string Symbol,
    decimal Price,
    decimal Quantity,
    decimal QuantityFilled,
    decimal? AverageFillPrice,
    bool IsBuy,
    OrderStatus Status);