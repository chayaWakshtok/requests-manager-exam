using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequestsManager.Application.Abstractions;
using RequestsManager.Application.Dtos;
using RequestsManager.Domain.Entities;
using RequestsManager.Domain.Enums;
using RequestsManager.Domain.Exceptions;

namespace RequestsManager.Application.Services;

public interface IRequestStatusService
{
    Task<RequestDto> UpdateStatusAsync(int id, RequestStatus status, string rowVersion, CancellationToken ct);
    Task<BulkUpdateStatusResult> BulkUpdateStatusAsync(BulkUpdateStatusRequest request, CancellationToken ct);
}

/// <summary>
/// Write side. Optimistic concurrency is enforced by the database: EF Core issues
/// UPDATE ... WHERE Id = @id AND RowVersion = @clientVersion, so of two concurrent updates
/// that started from the same version only one can affect a row. The audit row is inserted
/// in the same SaveChanges (same transaction) as the status change.
/// </summary>
public sealed class RequestStatusService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IRequestStatsService stats,
    TimeProvider time,
    ILogger<RequestStatusService> logger) : IRequestStatusService
{
    public async Task<RequestDto> UpdateStatusAsync(int id, RequestStatus status, string rowVersion, CancellationToken ct)
    {
        var entity = await db.Requests.FirstOrDefaultAsync(r => r.Id == id, ct)
                     ?? throw new NotFoundException("Request", id);

        var outcome = await TryApplyAsync(entity, status, RowVersionCodec.Decode(rowVersion), ct);
        switch (outcome)
        {
            case BulkItemOutcome.Conflict:
                logger.LogInformation("Concurrency conflict on request {RequestId}", id);
                throw new ConcurrencyConflictException(id);
            case BulkItemOutcome.InvalidTransition:
                throw new InvalidStatusTransitionException(id, entity.Status, status);
        }

        await stats.InvalidateAsync(ct);
        logger.LogInformation("Request {RequestId} status changed to {Status} by {User}", id, status, currentUser.Name);
        return entity.ToDto();
    }

    /// <summary>
    /// Partial success: every item is applied in its own short transaction and gets its own outcome.
    /// Items are independent requests, so one stale or missing item should not block the other 99
    /// (with All-or-Nothing a busy system would reject most bulk operations).
    /// </summary>
    public async Task<BulkUpdateStatusResult> BulkUpdateStatusAsync(BulkUpdateStatusRequest request, CancellationToken ct)
    {
        var status = request.Status!.Value;
        var ids = request.Items.Select(i => i.Id).ToList();

        // One round-trip to load all the requested rows (max 100).
        var entities = await db.Requests.Where(r => ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);

        var results = new List<BulkItemResult>(request.Items.Count);
        foreach (var item in request.Items)
        {
            if (!entities.TryGetValue(item.Id, out var entity))
            {
                results.Add(new BulkItemResult(item.Id, BulkItemOutcome.NotFound, null, "Request not found."));
                continue;
            }

            var outcome = await TryApplyAsync(entity, status, RowVersionCodec.Decode(item.RowVersion), ct);
            results.Add(outcome switch
            {
                BulkItemOutcome.Updated => new BulkItemResult(item.Id, outcome, RowVersionCodec.Encode(entity.RowVersion), null),
                BulkItemOutcome.Conflict => new BulkItemResult(item.Id, outcome, null, "Modified by another user."),
                _ => new BulkItemResult(item.Id, outcome, null, $"Transition from '{entity.Status}' to '{status}' is not allowed.")
            });
        }

        var succeeded = results.Count(r => r.Outcome == BulkItemOutcome.Updated);
        if (succeeded > 0)
            await stats.InvalidateAsync(ct);

        logger.LogInformation("Bulk status update to {Status} by {User}: {Succeeded}/{Requested} succeeded",
            status, currentUser.Name, succeeded, results.Count);

        return new BulkUpdateStatusResult(results.Count, succeeded, results.Count - succeeded, results);
    }

    /// <summary>Applies one status change + audit. Returns the outcome instead of throwing so bulk can reuse it.</summary>
    private async Task<BulkItemOutcome> TryApplyAsync(ServiceRequest entity, RequestStatus status, byte[] clientVersion, CancellationToken ct)
    {
        // Fail fast when the client already holds a stale version (no point in trying the UPDATE).
        if (!entity.RowVersion.AsSpan().SequenceEqual(clientVersion))
            return BulkItemOutcome.Conflict;

        if (!Domain.StatusTransitions.IsAllowed(entity.Status, status))
            return BulkItemOutcome.InvalidTransition;

        // The version the client saw becomes the concurrency token of the UPDATE's WHERE clause.
        db.Entry(entity).Property(e => e.RowVersion).OriginalValue = clientVersion;
        var history = entity.ChangeStatus(status, currentUser.Name, time.GetUtcNow().UtcDateTime);

        try
        {
            await db.SaveChangesAsync(ct);
            return BulkItemOutcome.Updated;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else updated the row between our read and our write.
            // Detach so the failed change is not retried by a later SaveChanges in the same scope.
            db.Entry(history).State = EntityState.Detached;
            db.Entry(entity).State = EntityState.Detached;
            return BulkItemOutcome.Conflict;
        }
    }
}
