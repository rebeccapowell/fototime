extern alias AppHostAlias;

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Aspire.Hosting;
using FotoTime.Tests.Utilities;

namespace FotoTime.Integration.Tests;

public class E2EIntegrationTest
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [RequiresDockerFact]
    public async Task WebServiceReturnsSuccessfulResponse()
    {
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<AppHostAlias::Program>(cancellationToken);

        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await builder.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        var webClient = app.CreateHttpClient("api");

        var response = await webClient.GetAsync("/health", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<HealthCheckResponse>(cancellationToken: cancellationToken);
        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload!.Status);
    }

    [RequiresDockerFact]
    public async Task DatabaseShouldBeHealthy()
    {
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<AppHostAlias::Program>(cancellationToken);

        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        await using var app = await builder.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        var webClient = app.CreateHttpClient("api");
        var response = await webClient.GetAsync("/health", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("postgres", content.ToLower());
        Assert.Contains("healthy", content.ToLower());
    }

    [RequiresDockerFact]
    public async Task TemporalPingEndpointReturnsTimestampPayload()
    {
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<AppHostAlias::Program>(cancellationToken);

        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await builder.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        var webClient = app.CreateHttpClient("api");

        var response = await webClient.GetAsync("/ping", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        Assert.True(payload.TryGetProperty("timestamp", out var timestampProperty));

        var timestamp = timestampProperty.GetDateTimeOffset();
        var now = DateTimeOffset.UtcNow;
        Assert.InRange(timestamp, now.AddMinutes(-5), now.AddMinutes(1));
    }
}

internal sealed record HealthCheckResponse(string Status);
