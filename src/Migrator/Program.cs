using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string not found.");

var context = new AppDbContextFactory().CreateDbContext([connectionString]);
await context.Database.MigrateAsync();