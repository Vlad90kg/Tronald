using System.Globalization;
using Tronald.Models;
using Tronald.Services;

namespace Tronald.Tests;

public class ImpactAnalyzerTests
{
    private readonly ImpactAnalyzer _analyzer = new();

    private static Headline HeadlineAt(string publishedAt) =>
        new("Test headline", DateTime.Parse(publishedAt, CultureInfo.InvariantCulture), "https://example.com", "us-news");

    private static PricePoint Price(string date, decimal close) =>
        new(DateOnly.Parse(date, CultureInfo.InvariantCulture), close);

    [Fact]
    public void Analyze_CalculatesChangeAndScore()
    {
        var prices = new[]
        {
            Price("2026-10-12", 100m), Price("2026-10-13", 100m),
            Price("2026-10-14", 97m),  Price("2026-10-15", 97m)
        };
        var headlines = new[] { HeadlineAt("2026-10-14T14:00:00") };

        var result = _analyzer.Analyze(headlines, prices);

        var impact = Assert.Single(result);
        Assert.Equal(-0.03m, impact.Change);
        Assert.Equal(3m, impact.Score);
        Assert.Equal(ImpactLevel.High, impact.Level);
    }

    [Fact]
    public void Analyze_WeekendHeadline_UsesFridayAndMondayPrices()
    {
        var prices = new[]
        {
            Price("2026-10-16", 100m), Price("2026-10-19", 102m), Price("2026-10-20", 102m)
        };
        var headlines = new[] { HeadlineAt("2026-10-17T10:00:00") };

        var impact = Assert.Single(_analyzer.Analyze(headlines, prices));

        Assert.Equal(new DateOnly(2026, 10, 16), impact.Before.Date);
        Assert.Equal(new DateOnly(2026, 10, 19), impact.After.Date);
        Assert.Equal(0.02m, impact.Change);
    }

    [Fact]
    public void Analyze_HeadlineAfterMarketClose_AffectsNextDay()
    {
        var prices = new[] { Price("2026-10-14", 100m), Price("2026-10-15", 105m) };
        var headlines = new[] { HeadlineAt("2026-10-14T21:00:00") };

        var impact = Assert.Single(_analyzer.Analyze(headlines, prices));

        Assert.Equal(new DateOnly(2026, 10, 14), impact.Before.Date);
        Assert.Equal(new DateOnly(2026, 10, 15), impact.After.Date);
        Assert.Equal(0.05m, impact.Change);
    }

    [Fact]
    public void Analyze_ResultsAreSortedByScoreDescending()
    {
        var prices = new[]
        {
            Price("2026-10-12", 100m), Price("2026-10-13", 101m), Price("2026-10-14", 91m)
        };
        var headlines = new[]
        {
            HeadlineAt("2026-10-13T12:00:00"),
            HeadlineAt("2026-10-14T12:00:00")
        };

        var result = _analyzer.Analyze(headlines, prices);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].Score > result[1].Score);
        Assert.Equal(new DateOnly(2026, 10, 14), result[0].After.Date);
    }

    [Fact]
    public void Analyze_HeadlineBeforeAllPrices_IsSkipped()
    {
        var prices = new[] { Price("2026-10-14", 100m), Price("2026-10-15", 101m) };
        var headlines = new[] { HeadlineAt("2026-10-01T12:00:00") };

        Assert.Empty(_analyzer.Analyze(headlines, prices));
    }

    [Fact]
    public void Analyze_NoHeadlines_ReturnsEmpty()
    {
        var prices = new[] { Price("2026-10-14", 100m), Price("2026-10-15", 101m) };

        Assert.Empty(_analyzer.Analyze([], prices));
    }

    [Fact]
    public void Analyze_LessThanTwoPrices_ReturnsEmpty()
    {
        var headlines = new[] { HeadlineAt("2026-10-14T12:00:00") };

        Assert.Empty(_analyzer.Analyze(headlines, [Price("2026-10-14", 100m)]));
    }

    [Theory]
    [InlineData("2026-10-14T19:59:00", "2026-10-14")]
    [InlineData("2026-10-14T20:00:00", "2026-10-15")]
    [InlineData("2026-10-14T23:30:00", "2026-10-15")]
    public void GetReactionDay_RespectsMarketClose(string publishedAt, string expected)
    {
        var day = ImpactAnalyzer.GetReactionDay(DateTime.Parse(publishedAt, CultureInfo.InvariantCulture));

        Assert.Equal(DateOnly.Parse(expected, CultureInfo.InvariantCulture), day);
    }

    [Fact]
    public void AverageAbsoluteDailyChange_AveragesUpAndDownMoves()
    {
        var prices = new[] { Price("2026-10-12", 100m), Price("2026-10-13", 110m), Price("2026-10-14", 99m) };

        Assert.Equal(0.1m, ImpactAnalyzer.AverageAbsoluteDailyChange(prices));
    }

    [Theory]
    [InlineData(2.5, ImpactLevel.High)]
    [InlineData(2.0, ImpactLevel.High)]
    [InlineData(1.0, ImpactLevel.Medium)]
    [InlineData(0.5, ImpactLevel.Low)]
    public void Level_IsDerivedFromScore(double score, ImpactLevel expected)
    {
        var impact = new HeadlineImpact(
            HeadlineAt("2026-10-14T12:00:00"),
            Price("2026-10-13", 100m), Price("2026-10-14", 100m),
            0m, (decimal)score);

        Assert.Equal(expected, impact.Level);
    }
}