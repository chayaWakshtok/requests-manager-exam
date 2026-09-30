using RequestsManager.Domain.Enums;
using RequestsManager.Domain.Exceptions;

namespace RequestsManager.Domain.Entities;

/// <summary>
/// A request (פנייה) submitted by an organization/employer.
/// Named ServiceRequest to avoid clashing with HTTP "Request" types.
/// </summary>
public class ServiceRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public RequestStatus Status { get; private set; } = RequestStatus.New;
    public RequestPriority Priority { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>SQL Server rowversion - changed by the database on every UPDATE.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<RequestStatusHistory> StatusHistory { get; set; } = new List<RequestStatusHistory>();

    /// <summary>
    /// Changes the status and records an audit entry.
    /// Throws <see cref="InvalidStatusTransitionException"/> when the transition is not allowed.
    /// </summary>
    public RequestStatusHistory ChangeStatus(RequestStatus newStatus, string changedBy, DateTime now)
    {
        if (!StatusTransitions.IsAllowed(Status, newStatus))
            throw new InvalidStatusTransitionException(Id, Status, newStatus);

        var history = new RequestStatusHistory
        {
            RequestId = Id,
            PreviousStatus = Status,
            NewStatus = newStatus,
            ChangedAt = now,
            ChangedBy = changedBy
        };

        Status = newStatus;
        UpdatedAt = now;
        StatusHistory.Add(history);
        return history;
    }

    /// <summary>Used by seeding/tests to create a request in a given state without history.</summary>
    public static ServiceRequest Create(string title, string organizationName, RequestPriority priority,
        string? assignedTo, DateTime createdAt, RequestStatus status = RequestStatus.New) => new()
    {
        Title = title,
        OrganizationName = organizationName,
        Priority = priority,
        AssignedTo = assignedTo,
        CreatedAt = createdAt,
        UpdatedAt = createdAt,
        Status = status
    };
}
