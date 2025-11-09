var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithVolume("pgdata", "/var/lib/postgresql/data");

var fototimeDb = postgres.AddDatabase("fototime");

var migrations = builder.AddProject<Projects.Migrator>("migrations")
    .WithReference(fototimeDb)
    .WaitFor(fototimeDb);

var api = builder.AddProject<Projects.Web>("api")
    .WithReference(fototimeDb)
    .WaitForCompletion(migrations);

await builder.Build().RunAsync();
