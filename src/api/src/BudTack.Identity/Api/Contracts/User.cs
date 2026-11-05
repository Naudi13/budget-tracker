namespace BudTack.Identity.Api.Contracts;

/// <summary>An account as the API returns it (identity.yaml <c>User</c>) — never with its password hash.</summary>
public sealed record User(Guid Id, string Name, string Email);
