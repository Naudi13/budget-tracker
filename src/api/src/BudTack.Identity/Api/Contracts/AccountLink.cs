namespace BudTack.Identity.Api.Contracts;

/// <summary>A single-use link's token, shown once (identity.yaml <c>AccountLink</c>, ADR-010).</summary>
public sealed record AccountLink(string Token, DateTimeOffset ExpiresAt);
