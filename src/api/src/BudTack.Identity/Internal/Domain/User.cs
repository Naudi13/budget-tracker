namespace BudTack.Identity.Internal.Domain;

/// <summary>
/// The one account a business signs in with (identity.users). The password
/// exists here only as <see cref="PasswordHash"/> (NFR-06).
/// </summary>
internal sealed record User(Guid Id, string Name, string Email, string PasswordHash);
