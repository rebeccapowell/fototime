using Infrastructure;
using Infrastructure.Temporal;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configure service defaults with OpenTelemetry
builder.AddServiceDefaults(
    metrics =>
    {
        metrics.AddMeter("WorkflowMetrics");
        metrics.AddMeter("Temporal.Client");
    },
    tracing =>
    {
        tracing
            .AddSource("Temporal.Client")
            .AddSource("Temporal.Workflow")
            .AddSource("Temporal.Activity");
    });

// Add services
var connectionString = builder.Configuration.GetConnectionString("fototime") ?? 
    throw new InvalidOperationException("Connection string 'fototime' not found.");

var temporalAddress = builder.Configuration["Services:temporal:tcp"] ?? 
    throw new InvalidOperationException("Temporal address not found.");

builder.Services.AddInfrastructureServices(connectionString, temporalAddress);

var app = builder.Build();

app.MapDefaultEndpoints();

// Temporal test endpoint
app.MapGet("/ping", async (ITemporalGateway gateway, CancellationToken cancellationToken) =>
{
    try
    {
        var timestamp = await gateway.RunPingWorkflowAsync(cancellationToken);
        return Results.Ok(new PingResponse(timestamp));
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/", () => "Hello World!");

app.Run();

internal sealed record PingResponse(DateTime Timestamp);

public partial class Program;
