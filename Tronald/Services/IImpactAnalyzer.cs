using Tronald.Models;

namespace Tronald.Services;

public interface IImpactAnalyzer
{
    IReadOnlyList<HeadlineImpact> Analyze(IReadOnlyList<Headline> headlines, IReadOnlyList<PricePoint> prices);
}