namespace BudTack.Transactions.Internal.Domain;

/// <summary>A budget of the business (transactions.budgets, ADR-009).</summary>
internal sealed record BudgetRow(
    Guid Id, Guid CategoryId, decimal Amount, DateOnly PeriodStart, DateOnly PeriodEnd);
