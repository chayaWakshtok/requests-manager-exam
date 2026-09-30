using FluentAssertions;
using RequestsManager.Domain;
using RequestsManager.Domain.Entities;
using RequestsManager.Domain.Enums;
using RequestsManager.Domain.Exceptions;

namespace RequestsManager.Tests.Unit;

public class StatusTransitionsTests
{
    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.InProgress, true)]
    [InlineData(RequestStatus.New, RequestStatus.Waiting, true)]
    [InlineData(RequestStatus.New, RequestStatus.Completed, false)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed, true)]
    [InlineData(RequestStatus.InProgress, RequestStatus.New, false)]
    [InlineData(RequestStatus.Waiting, RequestStatus.InProgress, true)]
    [InlineData(RequestStatus.Completed, RequestStatus.InProgress, false)]
    [InlineData(RequestStatus.InProgress, RequestStatus.InProgress, false)]
    public void IsAllowed_FollowsTheWorkflow(RequestStatus from, RequestStatus to, bool expected) =>
        StatusTransitions.IsAllowed(from, to).Should().Be(expected);

    [Fact]
    public void ChangeStatus_UpdatesStatusAndTimestamp_AndWritesAudit()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = created.AddDays(3);
        var request = ServiceRequest.Create("t", "org", RequestPriority.High, null, created);

        var history = request.ChangeStatus(RequestStatus.InProgress, "dana", now);

        request.Status.Should().Be(RequestStatus.InProgress);
        request.UpdatedAt.Should().Be(now);
        history.Should().BeEquivalentTo(new
        {
            PreviousStatus = RequestStatus.New, NewStatus = RequestStatus.InProgress, ChangedAt = now, ChangedBy = "dana"
        });
        request.StatusHistory.Should().ContainSingle();
    }

    [Fact]
    public void ChangeStatus_InvalidTransition_ThrowsAndLeavesEntityUnchanged()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = ServiceRequest.Create("t", "org", RequestPriority.Low, null, created, RequestStatus.Completed);

        var act = () => request.ChangeStatus(RequestStatus.New, "dana", created.AddDays(1));

        act.Should().Throw<InvalidStatusTransitionException>();
        request.Status.Should().Be(RequestStatus.Completed);
        request.UpdatedAt.Should().Be(created);
        request.StatusHistory.Should().BeEmpty();
    }
}
