using System.Collections.Generic;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Aspire.Hosting;

namespace FotoTime.Integration.Tests;

public class E2EIntegrationTest
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    [RequiresDocker]
    public async Task WebServiceReturnsSuccessfulResponse()
    {
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<AppHost.Program>(cancellationToken);

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

        var webClient = app.CreateHttpClient("web");

        var response = await webClient.GetAsync("/health", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<HealthCheckResponse>(cancellationToken: cancellationToken);
        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload!.Status);
    }

    [Fact]
    [RequiresDocker]
    public async Task DatabaseShouldBeHealthy()
    {
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<AppHost.Program>(cancellationToken);

        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        await using var app = await builder.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        var webClient = app.CreateHttpClient("web");
        var response = await webClient.GetAsync("/health", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<HealthCheckResponse>(cancellationToken: cancellationToken);
        Assert.NotNull(payload);

        Assert.Equal("Healthy", payload!.Status);
        Assert.True(payload.Results.TryGetValue("postgres", out var postgres), "Expected postgres entry in health response.");
        Assert.Equal("Healthy", postgres.Status);

        Assert.True(payload.Results.TryGetValue("temporal", out var temporal), "Expected temporal entry in health response.");
        Assert.Equal("Healthy", temporal.Status);
    }

    private sealed record HealthCheckResponse(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("results")] Dictionary<string, HealthCheckEntry> Results);

    private sealed record HealthCheckEntry(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("duration")] string Duration,
        [property: JsonPropertyName("exception")] string? Exception,
        [property: JsonPropertyName("data")] Dictionary<string, string?> Data);
}
