namespace BudTack.Transactions.Api.Contracts;

/// <summary>
/// One line of a transaction (transactions.yaml <c>TransactionDetail</c>). <c>Type</c>
/// is <c>income</c> or <c>expense</c>, read from the subcategory's category (BR-01).
/// </summary>
public sealed record TransactionDetail(Guid Id, Guid SubcategoryId, decimal Amount, string Type);
