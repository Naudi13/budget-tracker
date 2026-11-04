using BudTack.Identity.Api;
using BudTack.Transactions.Api;
using Microsoft.Extensions.DependencyInjection;

namespace BudTack.Transactions;

/// <summary>
/// The transactions module's public marker: tests name the assembly through it
/// (05-architecture/boundary-enforcement.md), and the host registers the
/// module with <see cref="AddTransactionsModule"/> after identity.
/// </summary>
public static class TransactionsModule
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped(provider => new TransactionsFacade(provider.GetRequiredService<IdentityFacade>()));
        return services;
    }
}
