namespace BudTack.Transactions.Api.Contracts;

/// <summary>The body of <c>POST /transactions/{id}/deletion</c>.</summary>
public sealed record TransactionDeletion(string? Reason);
