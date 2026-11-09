using BudTack.Identity.Api;
using BudTack.Transactions.Api;
using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BudTack.Transactions;

/// <summary>
/// The transactions module's public marker: tests name the assembly through it
/// (05-architecture/boundary-enforcement.md), and the host registers the
/// module with <see cref="AddTransactionsModule"/> after identity. Needs an
/// <c>NpgsqlDataSource</c> and logging from the host.
/// </summary>
public static class TransactionsModule
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICategoriesRepository, CategoriesRepository>();
        services.AddScoped(provider => new ManageCategories(
            provider.GetRequiredService<ICategoriesRepository>(),
            provider.GetRequiredService<ILogger<ManageCategories>>()));
        services.AddScoped<IMovementsRepository, MovementsRepository>();
        services.AddScoped<IAccountNames>(provider => new IdentityAccountNames(provider.GetRequiredService<IdentityFacade>()));
        services.AddScoped(provider => new ManageMovements(
            provider.GetRequiredService<IMovementsRepository>(),
            provider.GetRequiredService<IAccountNames>(),
            provider.GetRequiredService<ILogger<ManageMovements>>()));
        services.AddScoped<IBudgetsRepository, BudgetsRepository>();
        services.AddScoped(provider => new ManageBudgets(
            provider.GetRequiredService<IBudgetsRepository>(),
            provider.GetRequiredService<ICategoriesRepository>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<ILogger<ManageBudgets>>()));
        services.AddScoped(provider => new TransactionsFacade(
            provider.GetRequiredService<IdentityFacade>(),
            provider.GetRequiredService<ManageCategories>(),
            provider.GetRequiredService<ManageMovements>(),
            provider.GetRequiredService<ManageBudgets>()));
        return services;
    }
}
