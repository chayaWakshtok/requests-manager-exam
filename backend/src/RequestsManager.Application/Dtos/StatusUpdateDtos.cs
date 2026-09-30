using System.ComponentModel.DataAnnotations;
using RequestsManager.Domain.Enums;

namespace RequestsManager.Application.Dtos;

public sealed class UpdateStatusRequest : IValidatableObject
{
    [Required]
    public RequestStatus? Status { get; set; }

    /// <summary>The RowVersion (base64) the client last saw. Required to prevent lost updates.</summary>
    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not null && !Enum.IsDefined(Status.Value))
            yield return new ValidationResult("status is not a valid value.", [nameof(Status)]);
        if (!RowVersionCodec.TryDecode(RowVersion, out _))
            yield return new ValidationResult("rowVersion is not a valid version token.", [nameof(RowVersion)]);
    }
}

public sealed class BulkUpdateItem
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class BulkUpdateStatusRequest : IValidatableObject
{
    public const int MaxItems = 100;

    [Required]
    public RequestStatus? Status { get; set; }

    [Required, MinLength(1), MaxLength(MaxItems)]
    public List<BulkUpdateItem> Items { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not null && !Enum.IsDefined(Status.Value))
            yield return new ValidationResult("status is not a valid value.", [nameof(Status)]);
        if (Items.Select(i => i.Id).Distinct().Count() != Items.Count)
            yield return new ValidationResult("items must not contain duplicate ids.", [nameof(Items)]);
        if (Items.Any(i => !RowVersionCodec.TryDecode(i.RowVersion, out _)))
            yield return new ValidationResult("every item must have a valid rowVersion.", [nameof(Items)]);
    }
}

public enum BulkItemOutcome
{
    Updated,
    NotFound,
    Conflict,
    InvalidTransition
}

public sealed record BulkItemResult(int Id, BulkItemOutcome Outcome, string? RowVersion, string? Error);

public sealed record BulkUpdateStatusResult(int Requested, int Succeeded, int Failed, IReadOnlyList<BulkItemResult> Results);
