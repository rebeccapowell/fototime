using System;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__fototime")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'fototime' not found.");

await using var context = new AppDbContextFactory().CreateDbContext(new[] { connectionString });
await context.Database.MigrateAsync();
