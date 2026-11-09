namespace BudTack.Identity.Api.Contracts;

/// <summary>A signed-in session (identity.yaml <c>Session</c>): the bearer token, its lifetime in seconds and the account's role.</summary>
public sealed record Session(string Token, string TokenType, int ExpiresIn, string Role);
