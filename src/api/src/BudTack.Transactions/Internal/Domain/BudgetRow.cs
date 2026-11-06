namespace BudTack.Transactions.Internal.Domain;

/// <summary>A budget of one owner (transactions.budgets).</summary>
internal sealed record BudgetRow(
    Guid Id, Guid OwnedBy, Guid CategoryId, decimal Amount, DateOnly PeriodStart, DateOnly PeriodEnd);
