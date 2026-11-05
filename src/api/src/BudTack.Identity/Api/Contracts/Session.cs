namespace BudTack.Identity.Api.Contracts;

/// <summary>A signed-in session (identity.yaml <c>Session</c>): the bearer token and its lifetime in seconds.</summary>
public sealed record Session(string Token, string TokenType, int ExpiresIn);
