using BudTack.Identity.Api;
using Microsoft.Extensions.DependencyInjection;

namespace BudTack.Identity;

/// <summary>
/// The identity module's public marker: tests name the assembly through it
/// (05-architecture/boundary-enforcement.md), and the host registers the
/// module with <see cref="AddIdentityModule"/>.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped(_ => new IdentityFacade());
        return services;
    }
}
