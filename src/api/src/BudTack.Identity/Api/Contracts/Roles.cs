namespace BudTack.Identity.Api.Contracts;

/// <summary>The roles an account can have (identity.yaml <c>Role</c>, ADR-007).</summary>
public static class Roles
{
    public const string User = "USER";
    public const string Admin = "ADMIN";
}
