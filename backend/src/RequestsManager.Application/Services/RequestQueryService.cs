using Microsoft.EntityFrameworkCore;
using RequestsManager.Application.Abstractions;
using RequestsManager.Application.Dtos;
using RequestsManager.Domain.Entities;
using RequestsManager.Domain.Exceptions;

namespace RequestsManager.Application.Services;

public interface IRequestQueryService
{
    Task<PagedResult<RequestDto>> SearchAsync(RequestQuery query, CancellationToken ct);
    Task<RequestDto> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<StatusHistoryDto>> GetHistoryAsync(int id, CancellationToken ct);
}

/// <summary>
/// Read side. Every filter, sort and page is translated to SQL; only one page of rows
/// (max 100) is ever materialized in memory.
/// </summary>
public sealed class RequestQueryService(IAppDbContext db) : IRequestQueryService
{
    /// <summary>Shadow property mapped by the infrastructure layer to the computed search column.</summary>
    public const string SearchTextProperty = "SearchText";

    public async Task<PagedResult<RequestDto>> SearchAsync(RequestQuery query, CancellationToken ct)
    {
        var filtered = ApplyFilters(db.Requests.AsNoTracking(), query);

        var total = await filtered.CountAsync(ct);

        var rows = await ApplySort(filtered, query.SortBy, query.SortDir)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(RequestMappings.ToRow)
            .ToListAsync(ct);

        return new PagedResult<RequestDto>(rows.Select(r => r.ToDto()).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<RequestDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var row = await db.Requests.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(RequestMappings.ToRow)
            .FirstOrDefaultAsync(ct);

        return row?.ToDto() ?? throw new NotFoundException("Request", id);
    }

    public async Task<IReadOnlyList<StatusHistoryDto>> GetHistoryAsync(int id, CancellationToken ct)
    {
        if (!await db.Requests.AnyAsync(r => r.Id == id, ct))
            throw new NotFoundException("Request", id);

        return await db.StatusHistory.AsNoTracking()
            .Where(h => h.RequestId == id)
            .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
            .Select(h => new StatusHistoryDto(h.Id, h.PreviousStatus, h.NewStatus, h.ChangedAt, h.ChangedBy))
            .ToListAsync(ct);
    }

    internal static IQueryable<ServiceRequest> ApplyFilters(IQueryable<ServiceRequest> q, RequestQuery query)
    {
        if (query.Status is { Count: > 0 })
            q = q.Where(r => query.Status.Contains(r.Status));

        if (query.Priority is { Count: > 0 })
            q = q.Where(r => query.Priority.Contains(r.Priority));

        if (!string.IsNullOrWhiteSpace(query.OrganizationName))
        {
            var org = query.OrganizationName.Trim();
            q = q.Where(r => r.OrganizationName.StartsWith(org));
        }

        if (!string.IsNullOrWhiteSpace(query.AssignedTo))
        {
            var assignee = query.AssignedTo.Trim();
            q = q.Where(r => r.AssignedTo == assignee);
        }

        if (query.CreatedFrom is not null)
            q = q.Where(r => r.CreatedAt >= query.CreatedFrom);

        if (query.CreatedTo is not null)
            q = q.Where(r => r.CreatedAt <= query.CreatedTo);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Computed column UPPER(Title + ' ' + OrganizationName) in a binary collation - see performance.md.
            var term = query.Search.Trim().ToUpperInvariant();
            q = q.Where(r => EF.Property<string>(r, SearchTextProperty).Contains(term));
        }

        return q;
    }

    /// <summary>Whitelisted sort fields; Id is always the tie-breaker so paging is stable.</summary>
    internal static IQueryable<ServiceRequest> ApplySort(IQueryable<ServiceRequest> q, string sortBy, string sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<ServiceRequest> ordered = sortBy.ToLowerInvariant() switch
        {
            "updatedat" => desc ? q.OrderByDescending(r => r.UpdatedAt) : q.OrderBy(r => r.UpdatedAt),
            // Low-cardinality columns get CreatedAt as a secondary key, so the ORDER BY matches the
            // (Status, CreatedAt) / (Priority, CreatedAt) indexes and SQL reads only the page instead of sorting the table.
            "priority" => desc ? q.OrderByDescending(r => r.Priority).ThenByDescending(r => r.CreatedAt) : q.OrderBy(r => r.Priority).ThenBy(r => r.CreatedAt),
            "status" => desc ? q.OrderByDescending(r => r.Status).ThenByDescending(r => r.CreatedAt) : q.OrderBy(r => r.Status).ThenBy(r => r.CreatedAt),
            "title" => desc ? q.OrderByDescending(r => r.Title) : q.OrderBy(r => r.Title),
            "organizationname" => desc ? q.OrderByDescending(r => r.OrganizationName) : q.OrderBy(r => r.OrganizationName),
            _ => desc ? q.OrderByDescending(r => r.CreatedAt) : q.OrderBy(r => r.CreatedAt)
        };

        return desc ? ordered.ThenByDescending(r => r.Id) : ordered.ThenBy(r => r.Id);
    }
}
