namespace BudTack.Identity.Api.Contracts;

/// <summary>The body of <c>PATCH /users/{id}</c> (identity.yaml <c>UserStatusUpdate</c>).</summary>
public sealed record UserStatusUpdate(bool? IsActive);
