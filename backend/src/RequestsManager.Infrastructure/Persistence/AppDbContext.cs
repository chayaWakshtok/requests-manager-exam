using Microsoft.EntityFrameworkCore;
using RequestsManager.Application.Abstractions;
using RequestsManager.Domain.Entities;

namespace RequestsManager.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<ServiceRequest> Requests => Set<ServiceRequest>();
    public DbSet<RequestStatusHistory> StatusHistory => Set<RequestStatusHistory>();

    /// <summary>All timestamps are stored as UTC; mark them as UTC when read so JSON carries the 'Z' suffix.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}

internal sealed class UtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
