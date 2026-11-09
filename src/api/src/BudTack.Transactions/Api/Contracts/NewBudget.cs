namespace BudTack.Transactions.Api.Contracts;

/// <summary>The body of <c>POST /budgets</c> (budgets.yaml <c>NewBudget</c>). Fields are nullable: the module says what is missing.</summary>
public sealed record NewBudget(Guid? CategoryId, decimal? Amount, DateOnly? PeriodStart, DateOnly? PeriodEnd);
