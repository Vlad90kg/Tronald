using System.Net;
using System.Text.Json;
using Tronald.Exceptions;

namespace Tronald.Services;


public abstract class ApiClientBase(HttpClient http)
{
    protected abstract string ProviderName { get; }

    protected string RequireSetting(IConfiguration config, string name)
    {
        var value = config[name];
        if (string.IsNullOrWhiteSpace(value))
            throw new ApiException($"{ProviderName} API key is missing. Set {name} in user secrets.");
        return value;
    }

    protected async Task<T> GetJsonAsync<T>(string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(url);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"Could not reach the {ProviderName} API. Check your connection.", inner: ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new ApiException($"The {ProviderName} API took too long to respond.", inner: ex);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new ApiException($"{ProviderName} API rate limit reached. Try again later.", response.StatusCode);

        if (!response.IsSuccessStatusCode)
            throw new ApiException($"{ProviderName} API returned an error ({(int)response.StatusCode}).", response.StatusCode);

        try
        {
            var result = await response.Content.ReadFromJsonAsync<T>();
            return result ?? throw new ApiException($"{ProviderName} API returned an empty response.");
        }
        catch (JsonException ex)
        {
            throw new ApiException($"{ProviderName} API returned data in an unexpected format.", inner: ex);
        }
    }
}