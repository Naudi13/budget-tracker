using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// <c>GET /health</c>: <c>200</c> when the process and the database are up,
/// <c>503</c> otherwise. It reports the whole application, never a module
/// (13-operations/observability.md), and needs no authentication.
/// </summary>
internal static class HealthEndpoint
{
    public const string Path = "/health";

    public static IServiceCollection AddHealth(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", timeout: TimeSpan.FromSeconds(5));
        return services;
    }

    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(Path, new HealthCheckOptions { ResponseWriter = WriteAsync });
        return endpoints;
    }

    private static Task WriteAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(
            new
            {
                status = report.Status.ToString(),
                checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString()),
            },
            JsonSerializerOptions.Web);
}
