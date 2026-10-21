using Tronald.Models;

namespace Tronald.Services;

public interface IMarketDataService
{
    Task<IReadOnlyList<PricePoint>> GetPricesAsync(MarketSymbol symbol, DateOnly from, DateOnly to);
}