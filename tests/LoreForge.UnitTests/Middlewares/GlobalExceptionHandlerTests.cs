using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Text.Json;

namespace LoreForge.UnitTests.Middlewares;

public class GlobalExceptionHandlerTests
{

    [Fact(DisplayName = "Returns 500 with exception details when environment is Development")]
    public async Task Should_ReturnExceptionDetails_When_EnvironmentIsDevelopment()
    {
        var (handler, context, body) = CreateSut(isDevelopment: true);
        var exception = new InvalidOperationException("Something exploded");

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        var problem = await ReadProblemDetails(body);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Contains("Something exploded", problem.Detail);
    }

    [Fact(DisplayName = "Returns 500 with generic message when environment is Production")]
    public async Task Should_ReturnGenericMessage_When_EnvironmentIsProduction()
    {
        var (handler, context, body) = CreateSut(isDevelopment: false);
        var exception = new InvalidOperationException("Something exploded");

        await handler.TryHandleAsync(context, exception, CancellationToken.None);

        var problem = await ReadProblemDetails(body);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
        Assert.Equal("An unexpected error occurred. Please try again later.", problem.Detail);
        Assert.DoesNotContain("Something exploded", problem.Detail);
    }

    [Fact(DisplayName = "Sets response status code to 500")]
    public async Task Should_SetStatusCode500_When_ExceptionOccurs()
    {
        var (handler, context, _) = CreateSut(isDevelopment: false);

        await handler.TryHandleAsync(context, new Exception("boom"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact(DisplayName = "Returns true to signal exception was handled")]
    public async Task Should_ReturnTrue_When_ExceptionHandled()
    {
        var (handler, context, _) = CreateSut(isDevelopment: false);

        var handled = await handler.TryHandleAsync(context, new Exception("boom"), CancellationToken.None);

        Assert.True(handled);
    }
    
    private static (GlobalExceptionHandler handler, HttpContext context, MemoryStream body) CreateSut(bool isDevelopment)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(isDevelopment ? Environments.Development : Environments.Production);

        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, environment);

        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;

        return (handler, context, body);
    }

    private static async Task<ProblemDetails> ReadProblemDetails(MemoryStream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        return (await JsonSerializer.DeserializeAsync<ProblemDetails>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!;
    }
}
