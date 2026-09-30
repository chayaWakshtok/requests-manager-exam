using RequestsManager.Domain.Enums;

namespace RequestsManager.Application.Dtos;

public sealed record RequestDto(
    int Id,
    string Title,
    string OrganizationName,
    RequestStatus Status,
    RequestPriority Priority,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string RowVersion);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record StatusHistoryDto(
    long Id,
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    DateTime ChangedAt,
    string ChangedBy);

public sealed record CountByKey<TKey>(TKey Key, int Count);

public sealed record RequestStatsDto(
    int TotalCount,
    IReadOnlyList<CountByKey<RequestStatus>> ByStatus,
    IReadOnlyList<CountByKey<RequestPriority>> OpenByPriority,
    IReadOnlyList<CountByKey<string>> TopOrganizationsByOpenRequests,
    DateTime GeneratedAt);
