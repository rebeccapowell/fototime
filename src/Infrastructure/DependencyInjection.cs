using System.Diagnostics.Metrics;
using Infrastructure.HealthChecks;
using Infrastructure.Persistence;
using Infrastructure.Temporal.Activities;
using Infrastructure.Temporal.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Temporalio.Client;
using Temporalio.Extensions.DiagnosticSource;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string connectionString,
        string temporalAddress)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, opt =>
            {
                opt.EnableRetryOnFailure();
                opt.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });
        });

        // Register Temporal client and worker
        services
            .AddTemporalClient(temporalAddress, "default");

        services
            .AddHostedTemporalWorker("default")
            .AddWorkflow<PingWorkflow>()
            .AddScopedActivities<PingActivity>();

        // Activities
        services.AddTransient<IPingActivity, PingActivity>();

        // Health Checks
        services.AddHealthChecks()
            .AddCheck("postgres", new PostgresHealthCheck(connectionString), tags: new[] { "database" })
            .AddCheck<TemporalHealthCheck>("temporal", tags: new[] { "temporal" })
            .AddDbContextCheck<AppDbContext>("ef");

        return services;
    }
}
