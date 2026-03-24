using LoreForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

builder.Services.AddDbContext<LoreForgeDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();
