using Infrastructure;
using Infrastructure.Temporal.Workflows;
using System.Diagnostics.Metrics;
using Temporalio.Client;

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

// Temporal test endpoint
app.MapGet("/ping", async (ITemporalClient client) =>
{
    try
    {
        var workflow = await client.StartWorkflowAsync(
            (PingWorkflow wf) => wf.RunAsync(),
            new() { TaskQueue = "default", Id = $"ping-{Guid.NewGuid()}" }
        );

        var result = await workflow.GetResultAsync();
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/", () => "Hello World!");

app.Run();
