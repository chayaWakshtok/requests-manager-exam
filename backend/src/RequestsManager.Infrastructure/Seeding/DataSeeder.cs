using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequestsManager.Domain.Enums;
using RequestsManager.Infrastructure.Persistence;

namespace RequestsManager.Infrastructure.Seeding;

/// <summary>
/// Generates test data with SqlBulkCopy (100k rows in a few seconds).
/// A fixed random seed makes the data reproducible - re-running produces the same dataset.
/// </summary>
public sealed class DataSeeder(AppDbContext db, ILogger<DataSeeder> logger)
{
    private static readonly string[] OrgPrefixes =
        ["Acme", "Globex", "Initech", "Umbrella", "Stark", "Wayne", "Hooli", "Soylent", "Tyrell", "Cyberdyne",
         "Aperture", "Vandelay", "Wonka", "Gringotts", "Oscorp", "Massive", "Pied Piper", "Duff", "Krusty", "Monarch"];

    private static readonly string[] OrgSuffixes =
        ["Ltd", "Industries", "Group", "Technologies", "Logistics", "Holdings", "Services", "Systems", "Labs", "Partners",
         "Foods", "Retail", "Energy", "Health", "Finance", "Media", "Security", "Consulting", "Motors", "Construction",
         "Textiles", "Pharma", "Education", "Tourism", "Agro"];

    private static readonly string[] Subjects =
        ["Payroll report", "Employee registration", "Salary slip correction", "Pension deposit", "Work permit",
         "Tax certificate", "Vacation balance", "Sick leave approval", "Contract update", "Bank details change",
         "Invoice dispute", "Access request", "Account unlock", "Address change", "Overtime calculation",
         "Termination letter", "New branch setup", "Annual summary", "Grant application", "Insurance claim"];

    private static readonly string[] Qualifiers =
        ["urgent", "follow-up", "Q1", "Q2", "Q3", "Q4", "for review", "missing documents", "second request", "clarification"];

    public async Task SeedAsync(int count, bool reset, CancellationToken ct = default)
    {
        if (reset)
        {
            logger.LogInformation("Deleting existing data");
            // RESEED 0 only if the identity was used, otherwise the next id would be 0 instead of 1.
            await db.Database.ExecuteSqlRawAsync("""
                TRUNCATE TABLE RequestStatusHistory;
                DELETE FROM Requests;
                IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Requests') AND last_value IS NOT NULL)
                    DBCC CHECKIDENT ('Requests', RESEED, 0);
                """, ct);
        }
        else if (await db.Requests.AnyAsync(ct))
        {
            logger.LogInformation("Requests table already has data - skipping seed");
            return;
        }

        var table = BuildTable(count);

        var connection = (SqlConnection)db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(ct);
        try
        {
            using var bulk = new SqlBulkCopy(connection) { DestinationTableName = "Requests", BatchSize = 10_000, BulkCopyTimeout = 300 };
            foreach (DataColumn column in table.Columns)
                bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            await bulk.WriteToServerAsync(table, ct);
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }

        await db.Database.ExecuteSqlRawAsync("UPDATE STATISTICS Requests;", ct);
        logger.LogInformation("Seeded {Count} requests", count);
    }

    private static DataTable BuildTable(int count)
    {
        var random = new Random(20260930);
        var organizations = OrgPrefixes.SelectMany(p => OrgSuffixes.Select(s => $"{p} {s}")).ToArray(); // 500
        var assignees = Enumerable.Range(1, 40).Select(i => $"agent{i:00}").ToArray();
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        var table = new DataTable();
        table.Columns.Add("Title", typeof(string));
        table.Columns.Add("OrganizationName", typeof(string));
        table.Columns.Add("Status", typeof(byte));
        table.Columns.Add("Priority", typeof(byte));
        table.Columns.Add("AssignedTo", typeof(string));
        table.Columns.Add("CreatedAt", typeof(DateTime));
        table.Columns.Add("UpdatedAt", typeof(DateTime));

        for (var i = 0; i < count; i++)
        {
            var createdAt = now.AddMinutes(-random.Next(0, 2 * 365 * 24 * 60));
            // Older requests are more likely to be completed - a realistic skew for the aggregations.
            var ageDays = (now - createdAt).TotalDays;
            var status = random.NextDouble() < Math.Min(0.9, ageDays / 400)
                ? RequestStatus.Completed
                : (RequestStatus)random.Next(0, 3);
            var priority = (RequestPriority)(random.NextDouble() switch { < 0.5 => 0, < 0.85 => 1, _ => 2 });
            var assignee = status == RequestStatus.New && random.NextDouble() < 0.7
                ? null
                : assignees[random.Next(assignees.Length)];
            var updatedAt = status == RequestStatus.New ? createdAt : createdAt.AddHours(random.Next(1, 24 * 30));
            if (updatedAt > now) updatedAt = now;

            table.Rows.Add(
                $"{Subjects[random.Next(Subjects.Length)]} - {Qualifiers[random.Next(Qualifiers.Length)]} #{i + 1}",
                organizations[random.Next(organizations.Length)],
                (byte)status,
                (byte)priority,
                (object?)assignee ?? DBNull.Value,
                createdAt,
                updatedAt);
        }

        return table;
    }
}
