using RequestsManager.Domain.Enums;

namespace RequestsManager.Domain.Entities;

/// <summary>Audit row written in the same transaction as the status change.</summary>
public class RequestStatusHistory
{
    public long Id { get; set; }
    public int RequestId { get; set; }
    public RequestStatus PreviousStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}
