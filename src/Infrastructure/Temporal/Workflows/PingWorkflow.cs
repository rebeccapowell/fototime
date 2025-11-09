using Infrastructure.Temporal.Activities;
using Microsoft.Extensions.Logging;
using Temporalio.Workflows;

namespace Infrastructure.Temporal.Workflows;

[Workflow]
public interface IPingWorkflow
{
    [WorkflowRun]
    Task<DateTime> RunAsync();
}

public class PingWorkflow : IPingWorkflow
{
    private static readonly ActivityOptions Options = new()
    {
        StartToCloseTimeout = TimeSpan.FromMinutes(5)
    };

    public async Task<DateTime> RunAsync()
    {
        Workflow.Logger.LogInformation("Starting ping workflow");
        
        var result = await Workflow.ExecuteActivityAsync(
            (IPingActivity act) => act.PingAsync(),
            Options);

        Workflow.Logger.LogInformation("Ping workflow completed");
        
        return DateTime.Parse(result);
    }
}