namespace BudTack.Identity.Api.Contracts;

/// <summary>A new account and the link that activates it (identity.yaml <c>CreatedUser</c>).</summary>
public sealed record CreatedUser(User User, AccountLink Activation);
