using BudTack.Identity.Api;
using BudTack.Identity.Internal.Application;
using BudTack.Identity.Internal.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BudTack.Identity;

/// <summary>
/// The identity module's public marker: tests name the assembly through it
/// (05-architecture/boundary-enforcement.md), and the host registers the
/// module with <see cref="AddIdentityModule"/>. Needs an <c>NpgsqlDataSource</c>
/// and logging from the host.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IdentitySettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(settings);
        services.AddSingleton<PasswordHasher>();
        services.AddSingleton(provider => new TokenService(settings, provider.GetRequiredService<TimeProvider>()));
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped(provider => new SignUp(
            provider.GetRequiredService<IUsersRepository>(),
            provider.GetRequiredService<PasswordHasher>(),
            provider.GetRequiredService<ILogger<SignUp>>()));
        services.AddScoped(provider => new SignIn(
            provider.GetRequiredService<IUsersRepository>(),
            provider.GetRequiredService<PasswordHasher>(),
            provider.GetRequiredService<TokenService>(),
            provider.GetRequiredService<ILogger<SignIn>>()));
        services.AddScoped(provider => new ListUsers(provider.GetRequiredService<IUsersRepository>()));
        services.AddScoped(provider => new IdentityFacade(
            provider.GetRequiredService<SignUp>(),
            provider.GetRequiredService<SignIn>(),
            provider.GetRequiredService<TokenService>(),
            provider.GetRequiredService<ListUsers>()));
        return services;
    }
}
