namespace BudTack.Api.Platform.Web;

/// <summary>
/// The one origin allowed to call the API across origins, <c>CORS_ALLOWED_ORIGIN</c>
/// (ADR-008). The portal normally reaches the API through its own nginx, on one
/// origin (ADR-006), so this is the rule for anything that does not.
/// </summary>
internal static class PortalCors
{
    public const string DefaultOrigin = "http://localhost:8080";

    public static IServiceCollection AddPortalCors(this IServiceCollection services, IConfiguration configuration)
    {
        var configured = configuration["CORS_ALLOWED_ORIGIN"];
        var origin = string.IsNullOrWhiteSpace(configured) ? DefaultOrigin : configured.Trim().TrimEnd('/');

        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(origin)
            .WithMethods("GET", "POST", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type")));
        return services;
    }
}
