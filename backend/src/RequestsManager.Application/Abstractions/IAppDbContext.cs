using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RequestsManager.Domain.Entities;

namespace RequestsManager.Application.Abstractions;

/// <summary>
/// Thin abstraction over the EF Core context so the application layer does not depend on SQL Server.
/// EF Core itself already implements Unit of Work + Repository, so no extra repository layer is added.
/// </summary>
public interface IAppDbContext
{
    DbSet<ServiceRequest> Requests { get; }
    DbSet<RequestStatusHistory> StatusHistory { get; }
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
