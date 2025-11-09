using Infrastructure.Temporal;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Infrastructure.HealthChecks;

public sealed class TemporalHealthCheck : IHealthCheck
{
    private readonly ITemporalGateway _gateway;
    private readonly ILogger<TemporalHealthCheck> _logger;

    public TemporalHealthCheck(ITemporalGateway gateway, ILogger<TemporalHealthCheck> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _gateway.EnsureNamespaceAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Temporal health check failed");
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
