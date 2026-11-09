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
    /// HU-08: <c>GET /dashboard</c>. Totals and the recent list come from two
    /// reads of the transactions the viewer reaches; deleted ones count in neither
    /// (BR-04).
    /// </summary>
    public async Task<DashboardView> GetDashboardAsync(Viewer viewer, CancellationToken cancellationToken = default)
    {
        var totals = await Transactions.GetTotalsAsync(viewer, cancellationToken).ConfigureAwait(false);
        var recent = await Transactions
            .ListTransactionsAsync(viewer, page: 1, limit: RecentCount, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return new DashboardView(totals.Income, totals.Expenses, totals.Income - totals.Expenses, recent.Data);
    }
}
