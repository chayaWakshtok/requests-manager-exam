using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RequestsManager.Application.Abstractions;
using RequestsManager.Application.Dtos;
using RequestsManager.Domain.Enums;

namespace RequestsManager.Application.Services;

public interface IRequestStatsService
{
    Task<RequestStatsDto> GetStatsAsync(CancellationToken ct);
    Task InvalidateAsync(CancellationToken ct);
}

public sealed class StatsCacheOptions
{
    public const string SectionName = "StatsCache";
    public int AbsoluteExpirationSeconds { get; set; } = 60;
}

/// <summary>
/// Dashboard aggregations. They scan the whole table (100k+ rows) and are requested on every
/// page load, while the data only changes on a status update - so they are cached and the cache
/// entry is removed after every successful status change.
/// IDistributedCache is used so that switching to Redis (multi-instance) is configuration only.
/// </summary>
public sealed class RequestStatsService(
    IAppDbContext db,
    IDistributedCache cache,
    IOptions<StatsCacheOptions> options,
    TimeProvider time,
    ILogger<RequestStatsService> logger) : IRequestStatsService
{
    internal const string CacheKey = "requests:stats:v1";
    private const int TopOrganizations = 5;

    public async Task<RequestStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var cached = await cache.GetStringAsync(CacheKey, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<RequestStatsDto>(cached)!;

        var stats = await ComputeAsync(ct);

        await cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(stats), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(options.Value.AbsoluteExpirationSeconds)
        }, ct);

        logger.LogDebug("Request stats recomputed and cached");
        return stats;
    }

    public Task InvalidateAsync(CancellationToken ct) => cache.RemoveAsync(CacheKey, ct);

    private async Task<RequestStatsDto> ComputeAsync(CancellationToken ct)
    {
        var requests = db.Requests.AsNoTracking();

        var byStatus = await requests
            .GroupBy(r => r.Status)
            .Select(g => new CountByKey<RequestStatus>(g.Key, g.Count()))
            .ToListAsync(ct);

        var openByPriority = await requests
            .Where(r => r.Status != RequestStatus.Completed)
            .GroupBy(r => r.Priority)
            .Select(g => new CountByKey<RequestPriority>(g.Key, g.Count()))
            .ToListAsync(ct);

        var topOrganizations = await requests
            .Where(r => r.Status != RequestStatus.Completed)
            .GroupBy(r => r.OrganizationName)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Take(TopOrganizations)
            .Select(g => new CountByKey<string>(g.Key, g.Count()))
            .ToListAsync(ct);

        return new RequestStatsDto(
            byStatus.Sum(x => x.Count),
            Enum.GetValues<RequestStatus>().Select(s => new CountByKey<RequestStatus>(s, byStatus.FirstOrDefault(x => x.Key == s)?.Count ?? 0)).ToList(),
            Enum.GetValues<RequestPriority>().Select(p => new CountByKey<RequestPriority>(p, openByPriority.FirstOrDefault(x => x.Key == p)?.Count ?? 0)).ToList(),
            topOrganizations,
            time.GetUtcNow().UtcDateTime);
    }
}
