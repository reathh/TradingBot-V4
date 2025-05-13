using System.Collections.Concurrent;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;

namespace TradingBot.Services;

public record SymbolInfo(decimal StepSize, decimal MinQty, int QtyDecimals);

public interface ISymbolInfoCache
{
    Task<SymbolInfo> GetAsync(string symbol, CancellationToken ct = default);
    decimal RoundDownToStep(decimal quantity, SymbolInfo info);
}

public class SymbolInfoCache : ISymbolInfoCache
{
    private readonly ConcurrentDictionary<string, SymbolInfo> _cache = new();
    private readonly BinanceRestClient _publicRest = new();   // no auth needed

    public async Task<SymbolInfo> GetAsync(string symbol, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(symbol, out var info))
            return info;

        var exInfo = await _publicRest.SpotApi.ExchangeData.GetExchangeInfoAsync(symbols: new[] { symbol }, null, null, ct);

        if (!exInfo.Success)
            throw new Exception($"Can't get exchange-info for {symbol}: {exInfo.Error}");

        var s = exInfo.Data.Symbols.Single();
        var lot = s.Filters.OfType<BinanceSymbolLotSizeFilter>().Single();
        decimal step   = lot.StepSize;
        decimal minQty = lot.MinQuantity;
        int     dec    = GetDecimals(step);

        info = new SymbolInfo(step, minQty, dec);
        _cache.TryAdd(symbol, info);
        return info;
    }

    private static int GetDecimals(decimal d)
    {
        int[] bits = decimal.GetBits(d);
        int dec = (bits[3] >> 16) & 0xFF;
        return dec;
    }

    public decimal RoundDownToStep(decimal quantity, SymbolInfo info)
    {
        // Rounds down to the nearest allowed step size, and ensures >= min quantity
        var steps = Math.Floor(quantity / info.StepSize);
        var rounded = steps * info.StepSize;
        if (rounded < info.MinQty)
            return 0m;
        // Round to correct decimals to avoid floating point issues
        return Math.Round(rounded, info.QtyDecimals, MidpointRounding.ToZero);
    }
} 