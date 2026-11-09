namespace BudTack.Identity.Internal.Domain;

/// <summary>One row of identity.account_tokens (ADR-010): only the hash of the token.</summary>
internal sealed record AccountToken(
    Guid Id, Guid UserId, string Purpose, string TokenHash, DateTimeOffset ExpiresAt, Guid CreatedBy)
{
    public const string Activation = "ACTIVATION";
    public const string PasswordReset = "PASSWORD_RESET";
}
