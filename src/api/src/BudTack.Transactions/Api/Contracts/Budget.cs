namespace BudTack.Transactions.Api.Contracts;

/// <summary>A budget (budgets.yaml <c>Budget</c>): a limit for one category over a period.</summary>
public sealed record Budget(Guid Id, Guid CategoryId, decimal Amount, DateOnly PeriodStart, DateOnly PeriodEnd);
