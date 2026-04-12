using Amazon.BedrockRuntime;
using Amazon.DynamoDBv2;
using LoreForge.Api.Extensions;
using LoreForge.Api.Middlewares;
using LoreForge.Core.Ports;
using LoreForge.Infrastructure.Bedrock;
using LoreForge.Infrastructure.Bedrock.AgentTools;
using LoreForge.Infrastructure.DynamoDB;
using LoreForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

builder.Services.AddDbContext<LoreForgeDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector())
           .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IAmazonBedrockRuntime>(_ => new AmazonBedrockRuntimeClient());
builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
{
    var serviceUrl = builder.Configuration["DynamoDB:ServiceUrl"];
    if (!string.IsNullOrEmpty(serviceUrl))
    {
        var config = new AmazonDynamoDBConfig { ServiceURL = serviceUrl };
        return new AmazonDynamoDBClient("local", "local", config);
    }
    return new AmazonDynamoDBClient();
});
builder.Services.AddScoped<IEmbeddingService, BedrockEmbeddingService>();
builder.Services.AddScoped<IVectorStore, EfVectorStore>();
builder.Services.AddScoped<IConversationRepository, DynamoConversationRepository>();
builder.Services.AddScoped<IAgentService, BedrockAgentService>();
builder.Services.AddScoped<IAgentTool, SearchInspirationTool>();
builder.Services.AddScoped<IAgentTool, CheckWorldConsistencyTool>();
builder.Services.AddScoped<IAgentTool, SuggestCharacterArcTool>();
builder.Services.AddScoped<IAgentTool, AnalyzePlotStructureTool>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointHandlers(typeof(Program).Assembly);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyMethod()
        .AllowAnyHeader()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseSerilogRequestLogging();

app.UseCors("Frontend");
app.UseHttpsRedirection();

app.MapEndpoints(typeof(Program).Assembly);

app.Run();

public partial class Program { }
