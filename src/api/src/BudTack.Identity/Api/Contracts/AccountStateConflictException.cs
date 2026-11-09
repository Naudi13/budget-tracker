namespace BudTack.Identity.Api.Contracts;

/// <summary>The link asked for does not fit the account's state (409 CONFLICT).</summary>
public sealed class AccountStateConflictException(string message) : Exception(message);
