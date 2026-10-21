using System.Globalization;
using Tronald.Models;

namespace Tronald.Services;

public class FredService(HttpClient http, IConfiguration config)
    : ApiClientBase(http), IMarketDataService
{
    protected override string ProviderName => "FRED";

    public async Task<IReadOnlyList<PricePoint>> GetPricesAsync(MarketSymbol symbol, DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new ArgumentException("Start date must be before end date.");

        var key = RequireSetting(config, "Fred:ApiKey");
        var url = $"series/observations?series_id={symbol.ToFredSeriesId()}" +
                  $"&api_key={key}&file_type=json" +
                  $"&observation_start={from:yyyy-MM-dd}&observation_end={to:yyyy-MM-dd}";

        var result = await GetJsonAsync<FredObservationsResponse>(url);

        return result.Observations
            .Select(ToPricePoint)
            .OfType<PricePoint>()   
            .ToList();
    }

    
    public static PricePoint? ToPricePoint(FredObservation o)
    {
        if (!decimal.TryParse(o.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var close))
            return null;
        if (!DateOnly.TryParseExact(o.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;
        return new PricePoint(date, close);
    }
}