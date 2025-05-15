namespace TradingBot.Application.Common;

using Services;

public static class QuantityUtils
{
    public static decimal RoundDownToStep(decimal quantity, SymbolInfo info)
    {
        var steps = Math.Floor(quantity / info.StepSize);
        var rounded = steps * info.StepSize;
        if (rounded < info.MinQty)
            return 0m;
        return Math.Round(rounded, info.QtyDecimals, MidpointRounding.ToZero);
    }
} 