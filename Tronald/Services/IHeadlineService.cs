using Tronald.Models;

namespace Tronald.Services;

public interface IHeadlineService
{
    Task<IReadOnlyList<Headline>> SearchHeadlinesAsync(
        string query, DateOnly from, DateOnly to, int page = 1);
}