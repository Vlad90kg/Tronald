using System.Text.Json;
using Tronald.Exceptions;
using Tronald.Models;

namespace Tronald.Services;

public class CachedMarketDataService(
    FredService inner,
    IWebHostEnvironment env,
    ILogger<CachedMarketDataService> logger) : IMarketDataService
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    public async Task<IReadOnlyList<PricePoint>> GetPricesAsync(MarketSymbol symbol, DateOnly from, DateOnly to)
    {
        try
        {
            var fresh = await inner.GetPricesAsync(symbol, from, to);
            await SaveAsync(symbol, fresh);
            return fresh;
        }
        catch (ApiException ex)
        {
            var cached = (await LoadAsync(symbol))
                .Where(p => p.Date >= from && p.Date <= to)
                .OrderBy(p => p.Date)
                .ToList();

            if (cached.Count == 0)
                throw; 

            logger.LogWarning(ex, "FRED unavailable, serving {Symbol} from cache", symbol);
            return cached;
        }
    }

    private string CachePath(MarketSymbol symbol) =>
        Path.Combine(env.ContentRootPath, "Data", $"{symbol}.json");

    private async Task SaveAsync(MarketSymbol symbol, IReadOnlyList<PricePoint> prices)
    {
        await FileLock.WaitAsync();
        try
        {
            var merged = (await ReadFileAsync(symbol)).ToDictionary(p => p.Date);
            foreach (var p in prices)
                merged[p.Date] = p;

            Directory.CreateDirectory(Path.GetDirectoryName(CachePath(symbol))!);
            var json = JsonSerializer.Serialize(merged.Values.OrderBy(p => p.Date));
            await File.WriteAllTextAsync(CachePath(symbol), json);
        }
        finally
        {
            FileLock.Release();
        }
    }

    private async Task<List<PricePoint>> LoadAsync(MarketSymbol symbol)
    {
        await FileLock.WaitAsync();
        try { return await ReadFileAsync(symbol); }
        finally { FileLock.Release(); }
    }

    private async Task<List<PricePoint>> ReadFileAsync(MarketSymbol symbol)
    {
        var path = CachePath(symbol);
        if (!File.Exists(path))
            return [];
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<PricePoint>>(json) ?? [];
    }
}