using BudTack.Transactions.Api.Contracts;

namespace BudTack.Dashboard.Api.Contracts;

/// <summary>
/// The dashboard (dashboard.yaml <c>Dashboard</c>; named DashboardView in C# so it never reads as the namespace): the three KPIs and the five
/// most recent transactions. The net balance is income minus expenses, computed
/// on read (BR-03).
/// </summary>
public sealed record DashboardView(
    decimal TotalIncome, decimal TotalExpenses, decimal NetBalance, IReadOnlyList<Transaction> RecentTransactions);
