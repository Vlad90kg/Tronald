using System.Net;
using Microsoft.Extensions.Configuration;
using Tronald.Exceptions;
using Tronald.Models;
using Tronald.Services;
using Tronald.Tests.Fakes;

namespace Tronald.Tests;

public class FredServiceTests
{
    private static readonly DateOnly From = new(2026, 10, 1);
    private static readonly DateOnly To = new(2026, 10, 20);

    private static IConfiguration Config(string? key = "test-key") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Fred:ApiKey"] = key })
            .Build();

    private static FredService CreateService(HttpMessageHandler handler, IConfiguration? config = null) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.stlouisfed.org/fred/") }, config ?? Config());

    [Fact]
    public async Task GetPricesAsync_ParsesPricesAndSkipsMissingValues()
    {
        const string json = """
            {"observations":[
                {"date":"2026-10-14","value":"70.38"},
                {"date":"2026-10-15","value":"."}
            ]}
            """;
        var service = CreateService(new FakeHttpMessageHandler(HttpStatusCode.OK, json));

        var prices = await service.GetPricesAsync(MarketSymbol.WtiOil, From, To);

        var price = Assert.Single(prices);
        Assert.Equal(new DateOnly(2026, 10, 14), price.Date);
        Assert.Equal(70.38m, price.Close);
    }

    [Fact]
    public async Task GetPricesAsync_SendsCorrectSeriesId()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"observations":[]}""");
        var service = CreateService(handler);

        await service.GetPricesAsync(MarketSymbol.SP500, From, To);

        Assert.Contains("series_id=SP500", handler.LastRequest!.RequestUri!.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetPricesAsync_ErrorStatus_ThrowsApiException(HttpStatusCode status)
    {
        var service = CreateService(new FakeHttpMessageHandler(status, "{}"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => service.GetPricesAsync(MarketSymbol.WtiOil, From, To));

        Assert.Equal(status, ex.StatusCode);
    }

    [Fact]
    public async Task GetPricesAsync_HtmlMaintenancePage_ThrowsApiException()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "<html>We'll be back soon!</html>", "text/html");
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<ApiException>(() => service.GetPricesAsync(MarketSymbol.WtiOil, From, To));

        Assert.Contains("unavailable", ex.Message);
    }

    [Fact]
    public async Task GetPricesAsync_MalformedJson_ThrowsApiException()
    {
        var service = CreateService(new FakeHttpMessageHandler(HttpStatusCode.OK, "{ not json"));

        await Assert.ThrowsAsync<ApiException>(() => service.GetPricesAsync(MarketSymbol.WtiOil, From, To));
    }

    [Fact]
    public async Task GetPricesAsync_NoNetwork_ThrowsApiException()
    {
        var service = CreateService(new ThrowingHttpMessageHandler());

        await Assert.ThrowsAsync<ApiException>(() => service.GetPricesAsync(MarketSymbol.WtiOil, From, To));
    }

    [Fact]
    public async Task GetPricesAsync_MissingApiKey_ThrowsApiException()
    {
        var service = CreateService(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"), Config(key: null));

        var ex = await Assert.ThrowsAsync<ApiException>(() => service.GetPricesAsync(MarketSymbol.WtiOil, From, To));

        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public async Task GetPricesAsync_StartAfterEnd_ThrowsArgumentException()
    {
        var service = CreateService(new FakeHttpMessageHandler(HttpStatusCode.OK, "{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetPricesAsync(MarketSymbol.WtiOil, To, From));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("")]
    [InlineData("abc")]
    public void ToPricePoint_InvalidValue_ReturnsNull(string value)
    {
        Assert.Null(FredService.ToPricePoint(new FredObservation("2026-10-14", value)));
    }
}