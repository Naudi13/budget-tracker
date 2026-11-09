namespace BudTack.Transactions.Api.Contracts;

/// <summary>
/// The category or subcategory does not exist, or belongs to another user (404
/// NOT_FOUND). The two are never told apart (07-api/rest-conventions.md).
/// </summary>
public sealed class TransactionsNotFoundException(string message) : Exception(message);
