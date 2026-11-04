using BudTack.Dashboard.Api;
using BudTack.Transactions.Api;
using Microsoft.Extensions.DependencyInjection;

namespace BudTack.Dashboard;

/// <summary>
/// The dashboard module's public marker: tests name the assembly through it
/// (05-architecture/boundary-enforcement.md), and the host registers the
/// module with <see cref="AddDashboardModule"/> after transactions.
/// </summary>
public static class DashboardModule
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped(provider => new DashboardFacade(provider.GetRequiredService<TransactionsFacade>()));
        return services;
    }
}
