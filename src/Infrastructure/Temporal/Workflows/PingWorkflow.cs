using Infrastructure.Temporal.Activities;
using Microsoft.Extensions.Logging;
using Temporalio.Workflows;

#pragma warning disable IDE0040
namespace Infrastructure.Temporal.Workflows
{
    public interface IPingWorkflow
    {
        Task<DateTime> RunAsync();
    }

    [Workflow]
    public class PingWorkflow : IPingWorkflow
    {
        private static readonly ActivityOptions Options = new()
        {
            StartToCloseTimeout = TimeSpan.FromMinutes(5)
        };

        [WorkflowRun]
        public async Task<DateTime> RunAsync()
        {
            Workflow.Logger.LogInformation("Starting ping workflow");

            var result = await Workflow.ExecuteActivityAsync(
                (IPingActivity act) => act.PingAsync(),
                Options);

            Workflow.Logger.LogInformation("Ping workflow completed");

            return result;
        }
    }
}
#pragma warning restore IDE0040
