using CommunityToolkit.Aspire.Hosting.MailPit;
using InfinityFlow.Aspire.Temporal;

var builder = DistributedApplication.CreateBuilder(args);
var app = CreateApplication(builder);
app.Run();

public partial class Program
{
    public static DistributedApplication CreateApplication(IDistributedApplicationBuilder builder)
    {
        var postgres = builder.AddPostgres("postgres")
            .WithVolume("pgdata", "/var/lib/postgresql/data");

        var fototimeDb = postgres.AddDatabase("fototime");

        var mailpit = builder.AddMailPit("mailpit")
            .WithDataVolume("mailpit-data");

        var temporal = builder.AddTemporalServerContainer("temporal", b => b
            .WithPort(7233)
            .WithHttpPort(7234)
            .WithMetricsPort(7235)
            .WithUiPort(8233)
            .WithLogLevel(LogLevel.Info));

        var migrations = builder.AddProject<Projects.Migrator>("migrations")
            .WithReference(fototimeDb)
            .WaitFor(fototimeDb);

        var api = builder.AddProject<Projects.Web>("api")
            .WithReference(fototimeDb)
            .WithReference(temporal)
            .WithReference(mailpit)
            .WaitForCompletion(migrations);

        return builder.Build();
    }
}
