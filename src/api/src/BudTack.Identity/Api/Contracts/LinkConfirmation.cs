namespace BudTack.Identity.Api.Contracts;

/// <summary>The body of <c>POST /account-links/confirm</c>.</summary>
public sealed record LinkConfirmation(string? Token, string? Password);
