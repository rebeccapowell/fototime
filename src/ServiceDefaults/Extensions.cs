using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
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
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthResponse
        }).AllowAnonymous();

        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = _ => false
        }).AllowAnonymous();

        return app;
    }

    private static readonly JsonSerializerOptions HealthSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static Task WriteDetailedHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            Results = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthCheckEntry
                {
                    Status = entry.Value.Status.ToString(),
                    Description = entry.Value.Description,
                    Duration = entry.Value.Duration.ToString(),
                    Exception = entry.Value.Exception?.Message,
                    Data = entry.Value.Data.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value?.ToString())
                })
        };

        var payload = JsonSerializer.Serialize(response, HealthSerializerOptions);
        return context.Response.WriteAsync(payload);
    }

    private sealed class HealthCheckResponse
    {
        public string Status { get; set; } = string.Empty;

        public Dictionary<string, HealthCheckEntry> Results { get; set; } = new();
    }

    private sealed class HealthCheckEntry
    {
        public string Status { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Duration { get; set; } = string.Empty;

        public string? Exception { get; set; }

        public Dictionary<string, string?> Data { get; set; } = new();
    }
}
