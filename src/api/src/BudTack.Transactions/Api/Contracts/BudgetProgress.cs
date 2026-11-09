namespace BudTack.Transactions.Api.Contracts;

/// <summary>How spending compares with the budgets in force today (dashboard.yaml <c>BudgetProgress</c>, BR-08).</summary>
public sealed record BudgetProgress(decimal Amount, decimal Spent, decimal Available);
