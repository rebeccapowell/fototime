extern alias AppHostAlias;

using System.Net;
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

    [RequiresDockerFact]
    public async Task AnonymousUserIsRedirectedToLogin()
    {
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<AppHostAlias::Program>(cancellationToken);

        await using var app = await builder.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        var baseClient = app.CreateHttpClient("api");
        Assert.NotNull(baseClient.BaseAddress);

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };

        using var unauthClient = new HttpClient(handler)
        {
            BaseAddress = baseClient.BaseAddress
        };

        var response = await unauthClient.GetAsync("/", cancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("connect/authorize", response.Headers.Location!.OriginalString);
    }

    [RequiresDockerFact]
    public async Task SecurityHeadersAreAddedToResponses()
    {
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<AppHostAlias::Program>(cancellationToken);

        await using var app = await builder.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        var webClient = app.CreateHttpClient("api");

        var response = await webClient.GetAsync("/ping", cancellationToken);
        response.EnsureSuccessStatusCode();

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var cspValues));
        Assert.Contains("default-src 'self'", cspValues);

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
    }
}

internal sealed record HealthCheckResponse(string Status);
