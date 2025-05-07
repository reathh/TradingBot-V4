namespace TradingBot.Application.Common;

public static class CurrencyUtilities
{
    /// <summary>
    /// Extracts the base currency from a trading symbol (e.g., BTC from BTCUSDT)
    /// </summary>
    /// <param name="symbol">The trading pair symbol</param>
    /// <returns>The base currency part of the symbol</returns>
    public static string ExtractBaseCurrency(string symbol)
    {
        // Common quote currencies with more than 3 characters to identify where to split the trading pair
        var commonQuoteCurrencies = new[] { "USDT", "USDC", "FDUSD" };

        // Check if symbol ends with a common quote currency
        foreach (var quote in commonQuoteCurrencies)
        {
            if (symbol.EndsWith(quote, StringComparison.OrdinalIgnoreCase))
            {
                return symbol[..^quote.Length];
            }
        }

        // Check if symbol starts with a common quote currency
        foreach (var quote in commonQuoteCurrencies)
        {
            if (symbol.StartsWith(quote, StringComparison.OrdinalIgnoreCase))
            {
                return symbol[quote.Length..];
            }
        }

        // Default fallback - assume the first 3 characters are the base currency
        return symbol.Length > 3 ? symbol[..3] : symbol;
    }
}