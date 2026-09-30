using System.ComponentModel.DataAnnotations;
using RequestsManager.Domain.Enums;

namespace RequestsManager.Application.Dtos;

/// <summary>Query-string parameters for GET /api/requests. Validated automatically by [ApiController].</summary>
public sealed class RequestQuery : IValidatableObject
{
    public const int MaxPageSize = 100;

    public static readonly IReadOnlyCollection<string> SortableFields =
        ["createdAt", "updatedAt", "priority", "status", "title", "organizationName"];

    [Range(1, 100_000)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 20;

    public List<RequestStatus>? Status { get; set; }
    public List<RequestPriority>? Priority { get; set; }

    /// <summary>Prefix match (LIKE 'value%') so the index on OrganizationName can be used.</summary>
    [StringLength(200)]
    public string? OrganizationName { get; set; }

    /// <summary>Exact match.</summary>
    [StringLength(100)]
    public string? AssignedTo { get; set; }

    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }

    /// <summary>Free text, matched against Title and OrganizationName.</summary>
    [StringLength(100, MinimumLength = 2)]
    public string? Search { get; set; }

    public string SortBy { get; set; } = "createdAt";
    public string SortDir { get; set; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreatedFrom is not null && CreatedTo is not null && CreatedFrom > CreatedTo)
            yield return new ValidationResult("createdFrom must be earlier than or equal to createdTo.",
                [nameof(CreatedFrom), nameof(CreatedTo)]);

        if (!SortableFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult(
                $"sortBy must be one of: {string.Join(", ", SortableFields)}.", [nameof(SortBy)]);

        if (!string.Equals(SortDir, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("sortDir must be 'asc' or 'desc'.", [nameof(SortDir)]);

        // The model binder accepts any number for an enum (e.g. status=9), so check explicitly.
        if (Status?.Any(s => !Enum.IsDefined(s)) == true)
            yield return new ValidationResult("status contains an unknown value.", [nameof(Status)]);
        if (Priority?.Any(p => !Enum.IsDefined(p)) == true)
            yield return new ValidationResult("priority contains an unknown value.", [nameof(Priority)]);
    }
}
