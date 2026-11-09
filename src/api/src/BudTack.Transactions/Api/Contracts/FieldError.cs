namespace BudTack.Transactions.Api.Contracts;

/// <summary>One field the request got wrong, and why (for developers; the portal words it for the user).</summary>
public sealed record FieldError(string Field, string Message);
