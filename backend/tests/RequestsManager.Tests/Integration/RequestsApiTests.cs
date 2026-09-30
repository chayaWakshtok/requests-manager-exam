using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using RequestsManager.Application.Dtos;
using RequestsManager.Domain.Enums;

namespace RequestsManager.Tests.Integration;

/// <summary>End-to-end: HTTP -> controller -> services -> EF Core -> SQL Server.</summary>
[Collection(ApiCollection.Name)]
public class RequestsApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    // Every test uses its own organization name so tests don't see each other's rows.
    private static string UniqueOrg() => $"Org-{Guid.NewGuid():N}";

    [Fact]
    public async Task Search_FiltersSortsAndPaginatesOnTheServer()
    {
        var org = UniqueOrg();
        var baseDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        await factory.InsertAsync(
            Enumerable.Range(0, 5).Select(i => ApiFactory.NewRequest(org, RequestStatus.New, RequestPriority.High, $"Match {i}", baseDate.AddDays(i)))
                .Append(ApiFactory.NewRequest(org, RequestStatus.Completed, RequestPriority.High, "Other status", baseDate))
                .Append(ApiFactory.NewRequest(org, RequestStatus.New, RequestPriority.Low, "Other priority", baseDate))
                .ToArray());

        var response = await _client.GetAsync(
            $"/api/requests?organizationName={org}&status=New&priority=High&sortBy=createdAt&sortDir=desc&page=2&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ApiFactory.ReadAsync<PagedResult<RequestDto>>(response);
        page!.TotalCount.Should().Be(5);
        page.TotalPages.Should().Be(3);
        page.Items.Select(i => i.Title).Should().Equal("Match 2", "Match 1"); // newest first, second page
    }

    [Fact]
    public async Task Search_FreeText_MatchesTitleOrOrganization_CaseInsensitive()
    {
        var org = UniqueOrg();
        var marker = Guid.NewGuid().ToString("N")[..10];
        await factory.InsertAsync(
            ApiFactory.NewRequest(org, title: $"Pension {marker.ToUpperInvariant()} deposit"),
            ApiFactory.NewRequest($"{org}-{marker}", title: "Unrelated title"),
            ApiFactory.NewRequest(org, title: "No match here"));

        var page = await _client.GetFromJsonAsync<PagedResult<RequestDto>>(
            $"/api/requests?search={marker.ToLowerInvariant()}", ApiFactory.Json);

        page!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Search_InvalidParameters_Returns400WithDetails()
    {
        var response = await _client.GetAsync("/api/requests?pageSize=1000&sortBy=hack");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiFactory.ReadAsync<ValidationProblemDetails>(response);
        problem!.Errors.Should().ContainKey("PageSize");
    }

    [Fact]
    public async Task UpdateStatus_ConcurrentUpdatesWithSameVersion_OnlyOneSucceeds()
    {
        var created = (await factory.InsertAsync(ApiFactory.NewRequest(UniqueOrg())))[0];
        var version = Convert.ToBase64String(created.RowVersion);

        // Two users loaded the same version and both try to change the status at the same time.
        var responses = await Task.WhenAll(
            _client.PatchAsJsonAsync($"/api/requests/{created.Id}/status", new { status = "InProgress", rowVersion = version }),
            _client.PatchAsJsonAsync($"/api/requests/{created.Id}/status", new { status = "Waiting", rowVersion = version }));

        responses.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);

        var winner = await ApiFactory.ReadAsync<RequestDto>(responses.Single(r => r.IsSuccessStatusCode));
        winner!.RowVersion.Should().NotBe(version);
        winner.UpdatedAt.Should().BeAfter(created.UpdatedAt);

        var conflict = await ApiFactory.ReadAsync<ProblemDetails>(responses.Single(r => r.StatusCode == HttpStatusCode.Conflict));
        conflict!.Status.Should().Be(409);

        // Exactly one audit row - the loser left no trace (no lost update, no phantom history).
        var history = await _client.GetFromJsonAsync<List<StatusHistoryDto>>($"/api/requests/{created.Id}/history", ApiFactory.Json);
        history.Should().ContainSingle().Which.NewStatus.Should().Be(winner.Status);
    }

    [Fact]
    public async Task UpdateStatus_WithStaleVersion_Returns409()
    {
        var created = (await factory.InsertAsync(ApiFactory.NewRequest(UniqueOrg())))[0];
        var staleVersion = Convert.ToBase64String(created.RowVersion);

        (await _client.PatchAsJsonAsync($"/api/requests/{created.Id}/status", new { status = "InProgress", rowVersion = staleVersion }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await _client.PatchAsJsonAsync($"/api/requests/{created.Id}/status", new { status = "Completed", rowVersion = staleVersion }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateStatus_ErrorCases_ReturnTheRightStatusCodes()
    {
        var completed = (await factory.InsertAsync(ApiFactory.NewRequest(UniqueOrg(), RequestStatus.Completed)))[0];
        var version = Convert.ToBase64String(completed.RowVersion);

        (await _client.PatchAsJsonAsync("/api/requests/987654321/status", new { status = "InProgress", rowVersion = version }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await _client.PatchAsJsonAsync($"/api/requests/{completed.Id}/status", new { status = "InProgress", rowVersion = version }))
            .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        (await _client.PatchAsJsonAsync($"/api/requests/{completed.Id}/status", new { status = "Archived", rowVersion = version }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await _client.PatchAsJsonAsync($"/api/requests/{completed.Id}/status", new { status = "InProgress", rowVersion = "not-a-version" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BulkUpdate_PartialSuccess_ReportsOutcomePerItem()
    {
        var org = UniqueOrg();
        var rows = await factory.InsertAsync(
            ApiFactory.NewRequest(org),                          // will succeed
            ApiFactory.NewRequest(org),                          // will be sent with a stale version
            ApiFactory.NewRequest(org, RequestStatus.Completed)); // invalid transition
        var stale = Convert.ToBase64String(new byte[8]);

        var response = await _client.PostAsJsonAsync("/api/requests/bulk-status", new
        {
            status = "InProgress",
            items = new[]
            {
                new { id = rows[0].Id, rowVersion = Convert.ToBase64String(rows[0].RowVersion) },
                new { id = rows[1].Id, rowVersion = stale },
                new { id = rows[2].Id, rowVersion = Convert.ToBase64String(rows[2].RowVersion) },
                new { id = 987654321, rowVersion = stale }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ApiFactory.ReadAsync<BulkUpdateStatusResult>(response);
        result!.Succeeded.Should().Be(1);
        result.Failed.Should().Be(3);
        result.Results.Select(r => r.Outcome).Should().Equal(
            BulkItemOutcome.Updated, BulkItemOutcome.Conflict, BulkItemOutcome.InvalidTransition, BulkItemOutcome.NotFound);

        var untouched = await _client.GetFromJsonAsync<RequestDto>($"/api/requests/{rows[1].Id}", ApiFactory.Json);
        untouched!.Status.Should().Be(RequestStatus.New);
    }

    [Fact]
    public async Task Stats_AreRecomputedAfterAStatusChange()
    {
        var created = (await factory.InsertAsync(ApiFactory.NewRequest(UniqueOrg())))[0];
        var before = await _client.GetFromJsonAsync<RequestStatsDto>("/api/requests/stats", ApiFactory.Json);

        await _client.PatchAsJsonAsync($"/api/requests/{created.Id}/status",
            new { status = "InProgress", rowVersion = Convert.ToBase64String(created.RowVersion) });

        var after = await _client.GetFromJsonAsync<RequestStatsDto>("/api/requests/stats", ApiFactory.Json);
        int Count(RequestStatsDto s, RequestStatus status) => s.ByStatus.Single(x => x.Key == status).Count;

        // Without invalidation the cached (pre-update) numbers would be returned for 60 seconds.
        Count(after!, RequestStatus.InProgress).Should().Be(Count(before!, RequestStatus.InProgress) + 1);
    }
}
