namespace BudTack.Transactions.Api.Contracts;

/// <summary>
/// The request conflicts with what is stored (409 CONFLICT): a name already used
/// (HU-02), or a delete refused because the row is in use (BR-05, HU-03).
/// </summary>
public sealed class TransactionsConflictException(string message) : Exception(message);
