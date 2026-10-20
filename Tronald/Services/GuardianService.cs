using Tronald.Models;

namespace Tronald.Services;

public class GuardianService(HttpClient http, IConfiguration config)
    : ApiClientBase(http), IHeadlineService
{
    protected override string ProviderName => "Guardian";

    public async Task<IReadOnlyList<Headline>> SearchHeadlinesAsync(
        string query, DateOnly from, DateOnly to, int page = 1)
    {
        var key = RequireSetting(config, "Guardian:ApiKey");
        var url = $"search?q={Uri.EscapeDataString(query)}" +
                  $"&query-fields=headline" +
                  $"&from-date={from:yyyy-MM-dd}&to-date={to:yyyy-MM-dd}" +
                  $"&order-by=newest&page-size=50&page={page}&api-key={key}";

        var result = await GetJsonAsync<GuardianResponse>(url);
        return result.Response.Results
            .Select(ToHeadline)
            .ToList();
    }

    public static Headline ToHeadline(GuardianArticle a) =>
        new(a.WebTitle, a.WebPublicationDate, a.WebUrl, a.SectionId);
}