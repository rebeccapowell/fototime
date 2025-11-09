using Infrastructure.HealthChecks;
using Infrastructure.Temporal;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Unit;

public class TemporalHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenGatewaySucceeds()
    {
        var gateway = new FakeTemporalGateway();
        var healthCheck = new TemporalHealthCheck(gateway, NullLogger<TemporalHealthCheck>.Instance);

        var cancellationToken = TestContext.Current?.CancellationToken ?? default;
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.True(gateway.EnsureNamespaceInvocations > 0);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenGatewayThrows()
    {
        var gateway = new FakeTemporalGateway(new InvalidOperationException("boom"));
        var healthCheck = new TemporalHealthCheck(gateway, NullLogger<TemporalHealthCheck>.Instance);

        var cancellationToken = TestContext.Current?.CancellationToken ?? default;
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.True(gateway.EnsureNamespaceInvocations > 0);
    }

    private sealed class FakeTemporalGateway : ITemporalGateway
    {
        private readonly Exception? _exception;

        public FakeTemporalGateway()
        {
        }

        public FakeTemporalGateway(Exception exception)
        {
            _exception = exception;
        }

        public int EnsureNamespaceInvocations { get; private set; }

        public Task EnsureNamespaceAsync(CancellationToken cancellationToken = default)
        {
            EnsureNamespaceInvocations++;

            if (_exception != null)
            {
                throw _exception;
            }

            return Task.CompletedTask;
        }

        public Task<DateTime> RunPingWorkflowAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
