namespace BudTack.Identity.Api.Contracts;

/// <summary>The body of <c>POST /sessions</c> (identity.yaml <c>Credentials</c>).</summary>
public sealed record Credentials(string? Email, string? Password);
