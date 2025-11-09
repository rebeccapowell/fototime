using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Temporalio.Client;

namespace Infrastructure.HealthChecks;

public class TemporalHealthCheck : IHealthCheck
{
    private readonly ITemporalClient _client;
    private readonly ILogger<TemporalHealthCheck> _logger;

    public TemporalHealthCheck(ITemporalClient client, ILogger<TemporalHealthCheck> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Call describe namespace API to verify service is up and accessible
            var result = await _client.WorkflowService.DescribeNamespaceAsync(new() { Namespace = "default" }, new() { CancellationToken = cancellationToken });
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Temporal health check failed");
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
    }