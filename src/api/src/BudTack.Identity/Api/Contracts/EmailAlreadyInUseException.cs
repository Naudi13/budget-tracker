namespace BudTack.Identity.Api.Contracts;

/// <summary>Sign-up with an email another account uses (409 CONFLICT, TC-001).</summary>
public sealed class EmailAlreadyInUseException()
    : Exception("An account already uses this email. Sign in instead.");
