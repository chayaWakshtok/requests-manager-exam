using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RequestsManager.Domain;
using RequestsManager.Domain.Exceptions;

namespace RequestsManager.Api.Infrastructure;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            // The client went away (e.g. Angular cancelled a superseded search) - nothing to answer.
            logger.LogDebug("Request {Path} cancelled by client", context.Request.Path);
            context.Response.StatusCode = 499;
            return true;
        }

        var problem = exception switch
        {
            NotFoundException e => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound, Title = "Not found", Detail = e.Message
            },
            ConcurrencyConflictException e => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict, Title = "Concurrency conflict", Detail = e.Message,
                Extensions = { ["requestId"] = e.RequestId }
            },
            InvalidStatusTransitionException e => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity, Title = "Invalid status transition", Detail = e.Message,
                Extensions = { ["currentStatus"] = e.From.ToString(), ["allowed"] = StatusTransitions.NextStatuses(e.From).Select(s => s.ToString()) }
            },
            _ => null
        };

        if (problem is null)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            };
        }

        context.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context, ProblemDetails = problem, Exception = exception
        });
    }
}
