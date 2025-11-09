using Infrastructure.Temporal.Workflows;
using Microsoft.Extensions.Logging;
using Temporalio.Client;

#pragma warning disable IDE0040
namespace Infrastructure.Temporal
{
    public interface ITemporalGateway
    {
        Task<DateTime> RunPingWorkflowAsync(CancellationToken cancellationToken = default);

        Task EnsureNamespaceAsync(CancellationToken cancellationToken = default);
    }

    public sealed class TemporalGateway : ITemporalGateway
    {
        private readonly ITemporalClient _client;
        private readonly ILogger<TemporalGateway> _logger;

        public TemporalGateway(ITemporalClient client, ILogger<TemporalGateway> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<DateTime> RunPingWorkflowAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting ping workflow");

            var handle = await _client.StartWorkflowAsync(
                (PingWorkflow workflow) => workflow.RunAsync(),
                new() { TaskQueue = "default", Id = $"ping-{Guid.NewGuid()}" });

            var result = await handle.GetResultAsync();

            _logger.LogInformation("Ping workflow completed");

            return result;
        }

        public async Task EnsureNamespaceAsync(CancellationToken cancellationToken = default)
        {
            await _client.WorkflowService.DescribeNamespaceAsync(
                new() { Namespace = "default" },
                new() { CancellationToken = cancellationToken });
        }
    }
}
#pragma warning restore IDE0040
