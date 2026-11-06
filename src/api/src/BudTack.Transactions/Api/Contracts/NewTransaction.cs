namespace BudTack.Transactions.Api.Contracts;

/// <summary>The body of <c>POST /transactions</c> (transactions.yaml <c>NewTransaction</c>). Fields are nullable: the module says what is missing.</summary>
public sealed record NewTransaction(DateOnly? TransactionDate, string? Description, IReadOnlyList<NewTransactionDetail?>? Details);
