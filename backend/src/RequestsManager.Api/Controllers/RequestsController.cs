using Microsoft.AspNetCore.Mvc;
using RequestsManager.Application.Dtos;
using RequestsManager.Application.Services;

namespace RequestsManager.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Produces("application/json")]
public sealed class RequestsController(
    IRequestQueryService queries,
    IRequestStatusService statusService,
    IRequestStatsService statsService) : ControllerBase
{
    /// <summary>Server-side search: filters, free text, sorting and paging (max 100 per page).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<RequestDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<RequestDto>> Search([FromQuery] RequestQuery query, CancellationToken ct) =>
        queries.SearchAsync(query, ct);

    [HttpGet("{id:int}")]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<RequestDto> GetById(int id, CancellationToken ct) => queries.GetByIdAsync(id, ct);

    [HttpGet("{id:int}/history")]
    [ProducesResponseType<IReadOnlyList<StatusHistoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<StatusHistoryDto>> GetHistory(int id, CancellationToken ct) =>
        queries.GetHistoryAsync(id, ct);

    /// <summary>Aggregations for the dashboard (cached, invalidated on every status change).</summary>
    [HttpGet("stats")]
    [ProducesResponseType<RequestStatsDto>(StatusCodes.Status200OK)]
    public Task<RequestStatsDto> GetStats(CancellationToken ct) => statsService.GetStatsAsync(ct);

    /// <summary>
    /// Changes the status. The body must carry the rowVersion the client last saw;
    /// 409 when someone else changed the request since, 422 when the transition is not allowed.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<RequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<RequestDto> UpdateStatus(int id, UpdateStatusRequest body, CancellationToken ct) =>
        statusService.UpdateStatusAsync(id, body.Status!.Value, body.RowVersion, ct);

    /// <summary>
    /// Updates up to 100 requests. Partial success: always 200 with a per-item outcome
    /// (Updated / NotFound / Conflict / InvalidTransition) and totals.
    /// </summary>
    [HttpPost("bulk-status")]
    [ProducesResponseType<BulkUpdateStatusResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<BulkUpdateStatusResult> BulkUpdateStatus(BulkUpdateStatusRequest body, CancellationToken ct) =>
        statusService.BulkUpdateStatusAsync(body, ct);
}
