using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RequestsManager.Domain.Entities;
using RequestsManager.Domain.Enums;
using RequestsManager.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace RequestsManager.Tests.Integration;

/// <summary>
/// Boots the real API (in-memory TestServer) against a real SQL Server.
/// By default a throw-away SQL Server container is started with Testcontainers (requires Docker).
/// Set TEST_SQL_CONNECTION to use an existing server instead (a separate database is created).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer? _container =
        Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION") is null
            ? new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest").Build()
            : null;

    private string _connectionString = string.Empty;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task InitializeAsync()
    {
        if (_container is not null)
        {
            await _container.StartAsync();
            _connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
                { InitialCatalog = "RequestsManagerTests" }.ConnectionString;
        }
        else
        {
            _connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(
                    Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION"))
                { InitialCatalog = $"RequestsManagerTests_{Guid.NewGuid():N}" }.ConnectionString;
        }

        // Creating the client boots the app, which applies the EF migrations.
        _ = CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:SeedIfEmptyCount", "0");
    }

    /// <summary>Inserts requests directly through the DbContext and returns them (with ids and RowVersions).</summary>
    public async Task<List<ServiceRequest>> InsertAsync(params ServiceRequest[] requests)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Requests.AddRange(requests);
        await db.SaveChangesAsync();
        return requests.ToList();
    }

    public static ServiceRequest NewRequest(string org, RequestStatus status = RequestStatus.New,
        RequestPriority priority = RequestPriority.Medium, string title = "Test request", DateTime? createdAt = null) =>
        ServiceRequest.Create(title, org, priority, "tester", createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), status);

    public static Task<T?> ReadAsync<T>(HttpResponseMessage response) => response.Content.ReadFromJsonAsync<T>(Json);

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (_container is not null) await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
