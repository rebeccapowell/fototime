using FotoTime.Application.Common;
using FotoTime.Application.Groups;
using FotoTime.Application.Invitations;
using Infrastructure.Groups;
using Infrastructure.HealthChecks;
using Infrastructure.Invitations;
using Infrastructure.Persistence;
using Infrastructure.Temporal;
using Infrastructure.Temporal.Activities;
using Infrastructure.Temporal.Workflows;
using Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        // Options
        services.AddOptions<MailOptions>()
            .BindConfiguration(MailOptions.SectionName);

        // Core services
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, GuidGenerator>();
        services.AddSingleton<IInviteTokenGenerator, SecureInviteTokenGenerator>();
        services.AddSingleton<IGroupRepository, InMemoryGroupRepository>();
        services.AddSingleton<IInvitationWorkflowClient, InvitationWorkflowClient>();
        services.AddTransient<IInviteEmailService, InviteEmailService>();

        // Register Temporal client and worker
        services
            .AddTemporalClient(temporalAddress, "default");

        services
            .AddHostedTemporalWorker("default")
            .AddWorkflow<PingWorkflow>()
            .AddWorkflow<InvitationWorkflow>()
            .AddScopedActivities<PingActivity>()
            .AddScopedActivities<InvitationActivities>();

        services.AddSingleton<ITemporalGateway, TemporalGateway>();

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
