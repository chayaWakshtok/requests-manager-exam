using RequestsManager.Domain.Enums;

namespace RequestsManager.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string entity, object key)
    : DomainException($"{entity} '{key}' was not found.");

public sealed class InvalidStatusTransitionException(int requestId, RequestStatus from, RequestStatus to)
    : DomainException($"Request {requestId}: transition from '{from}' to '{to}' is not allowed.")
{
    public RequestStatus From { get; } = from;
    public RequestStatus To { get; } = to;
}

public sealed class ConcurrencyConflictException(int requestId)
    : DomainException($"Request {requestId} was modified by another user. Reload and try again.")
{
    public int RequestId { get; } = requestId;
}
