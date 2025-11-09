using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(
        this IHostApplicationBuilder builder,
        Action<MeterProviderBuilder>? configureMeterProvider = null,
        Action<TracerProviderBuilder>? configureTracerProvider = null)
    {
        builder.ConfigureOpenTelemetry();

        // Configure metrics
        if (configureMeterProvider != null)
        {
            builder.Services.ConfigureOpenTelemetryMeterProvider(configureMeterProvider);
        }

        // Configure tracing
        if (configureTracerProvider != null)
        {
            builder.Services.ConfigureOpenTelemetryTracerProvider(configureTracerProvider);
        }

        builder.AddDefaultHealthChecks();

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(rb => rb
                .AddService(builder.Environment.ApplicationName));

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddAspNetCoreInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();

        builder.Services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Period = TimeSpan.FromSeconds(30);
        });

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        return app;
    }
}