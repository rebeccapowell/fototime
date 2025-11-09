using System;
using System.Text.Json;
using Infrastructure.HealthChecks;
using Infrastructure.Temporal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Unit;

public class PingEndpointTests
{
    [Fact]
    public async Task PingEndpoint_ReturnsTimestamp()
    {
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;
        var expectedTimestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        using var factory = new TestWebApplicationFactory
        {
            Gateway = new FakeTemporalGateway(expectedTimestamp)
        };

        var client = factory.CreateClient();

        var response = await client.GetAsync("/ping", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);

        var timestamp = document.RootElement.GetProperty("timestamp").GetDateTime();

        Assert.Equal(expectedTimestamp, timestamp);
        Assert.Equal(1, factory.Gateway.PingCalls);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsTemporalHealthStatus()
    {
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;
        using var factory = new TestWebApplicationFactory
        {
            Gateway = new FakeTemporalGateway(DateTime.UtcNow)
        };

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);

        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());

        var temporalEntry = document.RootElement.GetProperty("results").GetProperty("temporal");
        Assert.Equal("Healthy", temporalEntry.GetProperty("status").GetString());
        Assert.True(factory.Gateway.EnsureNamespaceCalls > 0);
    }

    private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        public FakeTemporalGateway Gateway { get; set; } = new(DateTime.UtcNow);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(ITemporalGateway));
                services.AddSingleton<ITemporalGateway>(_ => Gateway);

                var hostedServiceDescriptors = services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                    .ToList();

                foreach (var descriptor in hostedServiceDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    options.Registrations.Clear();
                    options.Registrations.Add(new HealthCheckRegistration(
                        name: "temporal",
                        factory: sp => new TemporalHealthCheck(
                            sp.GetRequiredService<ITemporalGateway>(),
                            sp.GetRequiredService<ILogger<TemporalHealthCheck>>()),
                        failureStatus: default,
                        tags: new[] { "temporal" },
                        timeout: default));
                    options.Registrations.Add(new HealthCheckRegistration(
                        name: "postgres",
                        factory: _ => new StaticHealthCheck(),
                        failureStatus: default,
                        tags: new[] { "database" },
                        timeout: default));
                    options.Registrations.Add(new HealthCheckRegistration(
                        name: "ef",
                        factory: _ => new StaticHealthCheck(),
                        failureStatus: default,
                        tags: Array.Empty<string>(),
                        timeout: default));
                });
            });
        }
    }

    private sealed class FakeTemporalGateway : ITemporalGateway
    {
        private readonly DateTime _timestamp;

        public FakeTemporalGateway(DateTime timestamp)
        {
            _timestamp = timestamp;
        }

        public int EnsureNamespaceCalls { get; private set; }

        public int PingCalls { get; private set; }

        public Task EnsureNamespaceAsync(CancellationToken cancellationToken = default)
        {
            EnsureNamespaceCalls++;
            return Task.CompletedTask;
        }

        public Task<DateTime> RunPingWorkflowAsync(CancellationToken cancellationToken = default)
        {
            PingCalls++;
            return Task.FromResult(_timestamp);
        }
    }

    private sealed class StaticHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(HealthCheckResult.Healthy());
    }
}
