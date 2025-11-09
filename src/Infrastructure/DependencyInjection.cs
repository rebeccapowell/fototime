using Infrastructure.HealthChecks;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, opt =>
            {
                opt.EnableRetryOnFailure();
                opt.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
        });

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: new[] { "database" })
            .AddDbContextCheck<AppDbContext>("ef");

        return services;
    }
}
