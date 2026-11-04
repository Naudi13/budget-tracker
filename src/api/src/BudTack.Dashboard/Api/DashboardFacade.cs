using BudTack.Transactions.Api;

namespace BudTack.Dashboard.Api;

/// <summary>
/// The only entry point into the dashboard module. GetDashboard arrives with
/// HU-08; until then the facade exists so the host and the boundary tests can
/// depend on it.
/// </summary>
public sealed class DashboardFacade
{
    internal DashboardFacade(TransactionsFacade transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        Transactions = transactions;
    }

    /// <summary>The source of every figure: the dashboard computes on read and stores nothing.</summary>
    internal TransactionsFacade Transactions { get; }
}
