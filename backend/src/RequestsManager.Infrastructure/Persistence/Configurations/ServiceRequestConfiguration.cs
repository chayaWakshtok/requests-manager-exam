using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequestsManager.Application.Services;
using RequestsManager.Domain.Entities;

namespace RequestsManager.Infrastructure.Persistence.Configurations;

public sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    private const string SearchTextColumn = RequestQueryService.SearchTextProperty;

    public void Configure(EntityTypeBuilder<ServiceRequest> b)
    {
        b.ToTable("Requests");
        b.HasKey(r => r.Id);

        b.Property(r => r.Title).HasMaxLength(200).IsRequired();
        b.Property(r => r.OrganizationName).HasMaxLength(200).IsRequired();
        b.Property(r => r.AssignedTo).HasMaxLength(100);
        b.Property(r => r.Status).HasConversion<byte>();
        b.Property(r => r.Priority).HasConversion<byte>();
        b.Property(r => r.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(r => r.UpdatedAt).HasColumnType("datetime2(3)");

        // SQL Server rowversion: set by the DB on every UPDATE and used as the concurrency token.
        b.Property(r => r.RowVersion).IsRowVersion();

        b.HasMany(r => r.StatusHistory).WithOne().HasForeignKey(h => h.RequestId).OnDelete(DeleteBehavior.Cascade);

        // Free-text search column: an upper-cased copy of Title + OrganizationName in a binary collation.
        // LIKE '%term%' can't seek any index, but a BIN2 comparison is ~10x cheaper than the default
        // case-insensitive collation and the narrow index below is scanned instead of the whole table.
        // Not persisted - the index holds the values, so the table itself does not grow.
        b.Property<string>(SearchTextColumn)
            .HasMaxLength(401)
            .HasComputedColumnSql("UPPER([Title] + N' ' + [OrganizationName]) COLLATE Latin1_General_100_BIN2", stored: false);

        // Indexes - see docs/performance.md for the queries each one serves and the measured plans.
        // Keys are ascending on purpose: SQL Server appends the clustered key (Id ASC) to every
        // nonclustered index, so (CreatedAt ASC, Id ASC) read backwards gives exactly the API's
        // "ORDER BY CreatedAt DESC, Id DESC" without a sort. A DESC key would force a sort.

        // Default screen: filter by status, newest first. INCLUDE Priority covers the stats aggregations.
        b.HasIndex(r => new { r.Status, r.CreatedAt })
            .IncludeProperties(r => r.Priority)
            .HasDatabaseName("IX_Requests_Status_CreatedAt");

        // Unfiltered list sorted by date, and created-at range filters.
        b.HasIndex(r => r.CreatedAt).HasDatabaseName("IX_Requests_CreatedAt");

        // Sort by priority (then CreatedAt, Id) without a full sort.
        b.HasIndex(r => new { r.Priority, r.CreatedAt }).HasDatabaseName("IX_Requests_Priority_CreatedAt");

        // Organization prefix filter / sort, and the "top organizations" aggregation (INCLUDE Status).
        b.HasIndex(r => r.OrganizationName)
            .IncludeProperties(r => r.Status)
            .HasDatabaseName("IX_Requests_OrganizationName");

        // "My requests" - assignee, usually combined with a status filter.
        b.HasIndex(r => new { r.AssignedTo, r.Status }).HasDatabaseName("IX_Requests_AssignedTo_Status");

        b.HasIndex(SearchTextColumn).HasDatabaseName("IX_Requests_SearchText");
    }
}
