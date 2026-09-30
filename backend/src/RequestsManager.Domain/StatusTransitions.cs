using RequestsManager.Domain.Enums;

namespace RequestsManager.Domain;

/// <summary>
/// The allowed status workflow:
/// New -> InProgress | Waiting
/// InProgress -> Waiting | Completed
/// Waiting -> InProgress | Completed
/// Completed -> (terminal)
/// </summary>
public static class StatusTransitions
{
    private static readonly IReadOnlyDictionary<RequestStatus, RequestStatus[]> Allowed =
        new Dictionary<RequestStatus, RequestStatus[]>
        {
            [RequestStatus.New] = [RequestStatus.InProgress, RequestStatus.Waiting],
            [RequestStatus.InProgress] = [RequestStatus.Waiting, RequestStatus.Completed],
            [RequestStatus.Waiting] = [RequestStatus.InProgress, RequestStatus.Completed],
            [RequestStatus.Completed] = []
        };

    public static bool IsAllowed(RequestStatus from, RequestStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyList<RequestStatus> NextStatuses(RequestStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : [];
}
