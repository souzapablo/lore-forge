using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace LoreForge.Api.Extensions;

public interface IEndpoint
{
    static abstract void MapEndpoint(IEndpointRouteBuilder app);
}

public static class EndpointExtensions
{
    public static void AddEndpointHandlers(this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = GetEndpointTypes(assembly);
        foreach (var type in endpointTypes)
            services.AddScoped(type);
    }

    public static void MapEndpoints(this IEndpointRouteBuilder app, Assembly assembly)
    {
        var endpointTypes = GetEndpointTypes(assembly);
        foreach (var type in endpointTypes)
        {
            var method = type.GetMethod(nameof(IEndpoint.MapEndpoint),
                BindingFlags.Static | BindingFlags.Public);

            method?.Invoke(null, [app]);
        }
    }

    private static IEnumerable<Type> GetEndpointTypes(Assembly assembly) =>
        assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                     && t.IsAssignableTo(typeof(IEndpoint)));
}
