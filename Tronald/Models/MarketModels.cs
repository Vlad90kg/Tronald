namespace Tronald.Models;

public enum MarketSymbol
{
    WtiOil,
    BrentOil,
    SP500,
    Nasdaq,
    Vix
}

public record PricePoint(DateOnly Date, decimal Close);

public static class MarketSymbolExtensions
{
    private static readonly Dictionary<MarketSymbol, (string SeriesId, string Name)> Info = new()
    {
        [MarketSymbol.WtiOil]   = ("DCOILWTICO",   "WTI crude oil ($/barrel)"),
        [MarketSymbol.BrentOil] = ("DCOILBRENTEU", "Brent crude oil ($/barrel)"),
        [MarketSymbol.SP500]    = ("SP500",        "S&P 500"),
        [MarketSymbol.Nasdaq]   = ("NASDAQCOM",    "NASDAQ Composite"),
        [MarketSymbol.Vix]      = ("VIXCLS",       "VIX (volatility index)")
    };

    public static string ToFredSeriesId(this MarketSymbol symbol) => Info[symbol].SeriesId;
    public static string DisplayName(this MarketSymbol symbol) => Info[symbol].Name;
}