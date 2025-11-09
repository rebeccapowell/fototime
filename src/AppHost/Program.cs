using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateDefaultBuilder(args);

// Add services to the container
builder.ConfigureServices((context, services) =>
{
    // Add application services here
});

var host = builder.Build();
await host.RunAsync();