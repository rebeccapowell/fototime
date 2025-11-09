using Infrastructure;
using Infrastructure.Temporal;
using Infrastructure.Temporal.Activities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Unit;

public class InfrastructureServiceRegistrationTests
{
    private const string ConnectionString = "Host=localhost;Database=fototime;Username=postgres;Password=postgres";
    private const string TemporalAddress = "localhost:7233";

    [Fact]
    public void AddInfrastructureServices_RegistersTemporalWorkerAndGateway()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructureServices(ConnectionString, TemporalAddress);

        var workerRegistered = services.Any(
            descriptor => descriptor.ServiceType == typeof(IHostedService));

        Assert.True(workerRegistered);

        var gatewayDescriptor = services.FirstOrDefault(
            descriptor => descriptor.ServiceType == typeof(ITemporalGateway));

        Assert.NotNull(gatewayDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, gatewayDescriptor!.Lifetime);

        var activityDescriptor = services.FirstOrDefault(
            descriptor => descriptor.ServiceType == typeof(IPingActivity));

        Assert.NotNull(activityDescriptor);
        Assert.Equal(ServiceLifetime.Transient, activityDescriptor!.Lifetime);
    }
}
