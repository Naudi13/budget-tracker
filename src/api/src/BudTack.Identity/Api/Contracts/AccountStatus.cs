namespace BudTack.Identity.Api.Contracts;

/// <summary>Whether an account's owner has chosen a password yet (identity.yaml <c>User.status</c>).</summary>
public static class AccountStatus
{
    public const string Pending = "PENDING";
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
}
