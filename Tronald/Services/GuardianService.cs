using Tronald.Models;

namespace Tronald.Services;

public class GuardianService(HttpClient http, IConfiguration config)
{
    public async Task<List<GuardianArticle>> SearchHeadlinesAsync(
        string query, DateOnly from, DateOnly to, int page = 1)
    {
        var key = config["Guardian:ApiKey"];
        var url = $"search?q={Uri.EscapeDataString(query)}" +
                  $"&query-fields=headline" +
                  $"&from-date={from:yyyy-MM-dd}&to-date={to:yyyy-MM-dd}" +
                  $"&order-by=newest&page-size=50&page={page}&api-key={key}";

        var result = await http.GetFromJsonAsync<GuardianResponse>(url);
        return result?.Response.Results ?? [];
    }
}