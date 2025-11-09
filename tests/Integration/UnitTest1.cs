using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
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

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Healthy", content);
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

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var normalized = content.ToLowerInvariant();
        Assert.Contains("postgres", normalized);
        Assert.Contains("temporal", normalized);
        Assert.Contains("healthy", normalized);
    }
}
