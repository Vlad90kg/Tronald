using Tronald.Models;

namespace Tronald.Services;


public class ImpactAnalyzer : IImpactAnalyzer
{
    private const int MarketCloseHourUtc = 20;

    public IReadOnlyList<HeadlineImpact> Analyze(IReadOnlyList<Headline> headlines, IReadOnlyList<PricePoint> prices)
    {
        if (headlines.Count == 0 || prices.Count < 2)
            return [];

        var sorted = prices.OrderBy(p => p.Date).ToList();
        var typicalMove = AverageAbsoluteDailyChange(sorted);
        if (typicalMove == 0)
            return [];

        var results = new List<HeadlineImpact>();
        foreach (var headline in headlines)
        {
            var reactionDay = GetReactionDay(headline.PublishedAt);
            var before = sorted.LastOrDefault(p => p.Date < reactionDay);
            var after = sorted.FirstOrDefault(p => p.Date >= reactionDay);

            if (before is null || after is null)
                continue;

            var change = (after.Close - before.Close) / before.Close;
            results.Add(new HeadlineImpact(headline, before, after, change, Math.Abs(change) / typicalMove));
        }

        results.Sort(); 
        return results;
    }

    public static DateOnly GetReactionDay(DateTime publishedAtUtc)
    {
        var day = DateOnly.FromDateTime(publishedAtUtc);
        return publishedAtUtc.Hour >= MarketCloseHourUtc ? day.AddDays(1) : day;
    }

    public static decimal AverageAbsoluteDailyChange(IReadOnlyList<PricePoint> sorted)
    {
        decimal total = 0;
        for (var i = 1; i < sorted.Count; i++)
            total += Math.Abs((sorted[i].Close - sorted[i - 1].Close) / sorted[i - 1].Close);
        return total / (sorted.Count - 1);
    }
}