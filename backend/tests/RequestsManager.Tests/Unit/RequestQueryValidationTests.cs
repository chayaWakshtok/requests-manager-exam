using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using RequestsManager.Application.Dtos;
using RequestsManager.Domain.Enums;

namespace RequestsManager.Tests.Unit;

public class RequestQueryValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void DefaultQuery_IsValid() => Validate(new RequestQuery()).Should().BeEmpty();

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_OutsideLimit_IsRejected(int pageSize) =>
        Validate(new RequestQuery { PageSize = pageSize }).Should()
            .ContainSingle(r => r.MemberNames.Contains(nameof(RequestQuery.PageSize)));

    [Fact]
    public void DateRange_FromAfterTo_IsRejected() =>
        Validate(new RequestQuery { CreatedFrom = new DateTime(2026, 2, 1), CreatedTo = new DateTime(2026, 1, 1) })
            .Should().ContainSingle(r => r.MemberNames.Contains(nameof(RequestQuery.CreatedFrom)));

    [Fact]
    public void UnknownSortField_IsRejected() =>
        Validate(new RequestQuery { SortBy = "rowVersion; DROP TABLE Requests" })
            .Should().ContainSingle(r => r.MemberNames.Contains(nameof(RequestQuery.SortBy)));

    [Fact]
    public void UndefinedEnumValue_IsRejected() =>
        Validate(new RequestQuery { Status = [(RequestStatus)42] })
            .Should().ContainSingle(r => r.MemberNames.Contains(nameof(RequestQuery.Status)));

    [Fact]
    public void Bulk_MoreThan100Items_OrDuplicates_IsRejected()
    {
        var rv = Convert.ToBase64String(new byte[8]);
        var tooMany = new BulkUpdateStatusRequest
        {
            Status = RequestStatus.InProgress,
            Items = Enumerable.Range(1, 101).Select(i => new BulkUpdateItem { Id = i, RowVersion = rv }).ToList()
        };
        var duplicates = new BulkUpdateStatusRequest
        {
            Status = RequestStatus.InProgress,
            Items = [new BulkUpdateItem { Id = 1, RowVersion = rv }, new BulkUpdateItem { Id = 1, RowVersion = rv }]
        };

        Validate(tooMany).Should().Contain(r => r.MemberNames.Contains(nameof(BulkUpdateStatusRequest.Items)));
        Validate(duplicates).Should().ContainSingle(r => r.ErrorMessage!.Contains("duplicate"));
    }
}
