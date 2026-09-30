using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RequestsManager.Application.Services;

namespace RequestsManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Scoped: services depend on the scoped DbContext.
        services.AddScoped<IRequestQueryService, RequestQueryService>();
        services.AddScoped<IRequestStatusService, RequestStatusService>();
        services.AddScoped<IRequestStatsService, RequestStatsService>();

        services.Configure<StatsCacheOptions>(configuration.GetSection(StatsCacheOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
