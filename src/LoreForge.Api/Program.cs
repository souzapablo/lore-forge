using Amazon.BedrockRuntime;
using LoreForge.Api.Features.Logbook;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Bedrock;
using LoreForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

builder.Services.AddDbContext<LoreForgeDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector())
           .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IAmazonBedrockRuntime>(_ => new AmazonBedrockRuntimeClient());
builder.Services.AddScoped<IEmbeddingService, BedrockEmbeddingService>();

builder.Services.AddScoped<AddWorkHandler>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

AddWorkHandler.MapEndpoint(app);

app.Run();

public partial class Program { }
