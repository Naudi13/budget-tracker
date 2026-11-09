namespace BudTack.Identity.Api.Contracts;

/// <summary>No account has that id (404 NOT_FOUND).</summary>
public sealed class UserNotFoundException()
    : Exception("The account does not exist.");
