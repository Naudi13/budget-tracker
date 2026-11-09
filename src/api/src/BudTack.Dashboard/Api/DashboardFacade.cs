using BudTack.Dashboard.Api.Contracts;
using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;

namespace BudTack.Dashboard.Api;

/// <summary>
/// The only entry point into the dashboard module (09-modules/dashboard). It
/// stores nothing: every figure is read through <see cref="TransactionsFacade"/>.
/// </summary>
public sealed class DashboardFacade
{
    /// <summary>How many transactions the recent list shows (dashboard.yaml, maxItems).</summary>
    public const int RecentCount = 5;

    internal DashboardFacade(TransactionsFacade transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        Transactions = transactions;
    }

    /// <summary>The source of every figure: the dashboard computes on read and stores nothing.</summary>
    internal TransactionsFacade Transactions { get; }

    /// <summary>
    /// HU-08 and HU-11: <c>GET /dashboard</c>, for the whole business or one category.
    /// Totals and the recent list come from the transactions the viewer reaches; the
    /// budget figures are the business's (BR-08); deleted ones count in none (BR-04).
    /// An unknown category is not found.
    /// </summary>
    public async Task<DashboardView> GetDashboardAsync(
        Viewer viewer, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var budget = await Transactions.GetBudgetProgressAsync(categoryId, cancellationToken).ConfigureAwait(false);
        var totals = await Transactions.GetTotalsAsync(viewer, categoryId, cancellationToken).ConfigureAwait(false);
        var recent = await Transactions
            .ListTransactionsAsync(viewer, page: 1, limit: RecentCount, categoryId: categoryId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return new DashboardView(totals.Income, totals.Expenses, totals.Income - totals.Expenses, budget, recent.Data);
    }
}
