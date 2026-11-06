namespace BudTack.Transactions.Api.Contracts;

/// <summary>
/// The body of <c>PATCH /budgets/{budgetId}</c> (budgets.yaml <c>BudgetChanges</c>):
/// only the fields sent are changed, and at least one must be sent.
/// </summary>
public sealed record BudgetChanges(Guid? CategoryId, decimal? Amount, DateOnly? PeriodStart, DateOnly? PeriodEnd);
