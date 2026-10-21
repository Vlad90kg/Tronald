namespace Tronald.Models;

public enum ImpactLevel { Low, Medium, High }

public record HeadlineImpact(
    Headline Headline,
    PricePoint Before,
    PricePoint After,
    decimal Change,   
    decimal Score)   
    : IComparable<HeadlineImpact>
{
    public ImpactLevel Level => Score switch
    {
        >= 2m => ImpactLevel.High,
        >= 1m => ImpactLevel.Medium,
        _ => ImpactLevel.Low
    };

    public int CompareTo(HeadlineImpact? other) =>
        other is null ? -1 : other.Score.CompareTo(Score);
}