using BudTack.Identity.Api.Contracts;

namespace BudTack.Identity.Internal.Domain;

/// <summary>
/// An account (identity.users). The password exists here only as
/// <see cref="PasswordHash"/> (NFR-06), and not at all until the account is activated.
/// </summary>
internal sealed record User(Guid Id, string Name, string Email, string? PasswordHash, string Role, bool IsActive = true)
{
    public string Status =>
        !IsActive ? AccountStatus.Inactive
        : PasswordHash is null ? AccountStatus.Pending
        : AccountStatus.Active;
}
