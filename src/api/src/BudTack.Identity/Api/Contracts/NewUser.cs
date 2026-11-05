namespace BudTack.Identity.Api.Contracts;

/// <summary>The body of <c>POST /users</c> (identity.yaml <c>NewUser</c>). Fields are nullable: the module, not the binder, says what is missing.</summary>
public sealed record NewUser(string? Name, string? Email, string? Password);
