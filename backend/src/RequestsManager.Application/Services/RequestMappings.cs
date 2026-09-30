using System.Linq.Expressions;
using RequestsManager.Application.Dtos;
using RequestsManager.Domain.Entities;

namespace RequestsManager.Application.Services;

internal static class RequestMappings
{
    /// <summary>Projection used inside IQueryable so only the needed columns are read from the DB.</summary>
    public static readonly Expression<Func<ServiceRequest, RequestRow>> ToRow = r => new RequestRow(
        r.Id, r.Title, r.OrganizationName, r.Status, r.Priority, r.AssignedTo, r.CreatedAt, r.UpdatedAt, r.RowVersion);

    public static RequestDto ToDto(this RequestRow r) => new(
        r.Id, r.Title, r.OrganizationName, r.Status, r.Priority, r.AssignedTo, r.CreatedAt, r.UpdatedAt,
        RowVersionCodec.Encode(r.RowVersion));

    public static RequestDto ToDto(this ServiceRequest r) => new(
        r.Id, r.Title, r.OrganizationName, r.Status, r.Priority, r.AssignedTo, r.CreatedAt, r.UpdatedAt,
        RowVersionCodec.Encode(r.RowVersion));
}

/// <summary>Flat row read from SQL; RowVersion is encoded to base64 after materialization.</summary>
internal sealed record RequestRow(
    int Id, string Title, string OrganizationName,
    Domain.Enums.RequestStatus Status, Domain.Enums.RequestPriority Priority,
    string? AssignedTo, DateTime CreatedAt, DateTime UpdatedAt, byte[] RowVersion);
