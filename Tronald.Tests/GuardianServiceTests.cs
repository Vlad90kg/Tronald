using System.Net;
using Microsoft.Extensions.Configuration;
using Tronald.Exceptions;
using Tronald.Services;
using Tronald.Tests.Fakes;

namespace Tronald.Tests;

public class GuardianServiceTests
{
    private static readonly DateOnly From = new(2026, 10, 1);
    private static readonly DateOnly To = new(2026, 10, 20);

    private static IConfiguration Config(string? key = "test-key") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Guardian:ApiKey"] = key })
            .Build();

    private static GuardianService CreateService(HttpMessageHandler handler, IConfiguration? config = null) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://content.guardianapis.com/") }, config ?? Config());

    private const string OneArticleJson = """
        {"response":{"status":"ok","total":1,"pages":1,"currentPage":1,"results":[
            {"id":"us-news/1","webTitle":"Trump says oil prices will fall",
             "webPublicationDate":"2026-10-14T14:00:00Z",
             "webUrl":"https://www.theguardian.com/us-news/1","sectionId":"us-news"}
        ]}}
        """;

    [Fact]
    public async Task SearchHeadlinesAsync_MapsGuardianArticleToHeadline()
    {
        var service = CreateService(new FakeHttpMessageHandler(HttpStatusCode.OK, OneArticleJson));

        var headlines = await service.SearchHeadlinesAsync("Trump", From, To);

        var h = Assert.Single(headlines);
        Assert.Equal("Trump says oil prices will fall", h.Title);
        Assert.Equal("https://www.theguardian.com/us-news/1", h.Url);
        Assert.Equal("us-news", h.Section);
        Assert.Equal(new DateTime(2026, 10, 14, 14, 0, 0, DateTimeKind.Utc), h.PublishedAt);
    }

    [Fact]
    public async Task SearchHeadlinesAsync_EscapesQueryAndSendsDates()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, OneArticleJson);
        var service = CreateService(handler);

        await service.SearchHeadlinesAsync("Trump AND oil", From, To);

        var url = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.Contains("q=Trump%20AND%20oil", url);
        Assert.Contains("from-date=2026-10-01", url);
        Assert.Contains("to-date=2026-10-20", url);
    }

    [Fact]
    public async Task SearchHeadlinesAsync_RateLimited_ThrowsApiException()
    {
        var service = CreateService(new FakeHttpMessageHandler(HttpStatusCode.TooManyRequests, "{}"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => service.SearchHeadlinesAsync("Trump", From, To));

        Assert.Contains("rate limit", ex.Message);
    }

    [Fact]
    public async Task SearchHeadlinesAsync_MissingApiKey_ThrowsApiException()
    {
        var service = CreateService(new FakeHttpMessageHandler(HttpStatusCode.OK, OneArticleJson), Config(key: null));

        await Assert.ThrowsAsync<ApiException>(() => service.SearchHeadlinesAsync("Trump", From, To));
    }
}