using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RequestsManager.Application.Abstractions;
using RequestsManager.Infrastructure.Persistence;
using RequestsManager.Infrastructure.Seeding;

namespace RequestsManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
                               ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        // DbContext is scoped (one per HTTP request) - the default and the correct lifetime for EF Core.
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString, sql => sql
            .EnableRetryOnFailure(3)
            // Translate small list filters (status/priority) as IN (...) instead of OPENJSON,
            // so the API also works on LocalDB/Express or databases at compatibility level < 130.
            .UseCompatibilityLevel(120)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<DataSeeder>();

        // Single instance: in-process distributed cache. Multiple instances: set Redis:ConnectionString
        // and every instance shares the same cache, so an invalidation on one is seen by all.
        var redis = configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(redis))
            services.AddDistributedMemoryCache();
        else
            services.AddStackExchangeRedisCache(o => { o.Configuration = redis; o.InstanceName = "requests-manager:"; });

        return services;
    }
}
