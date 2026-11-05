namespace BudTack.Identity.Api.Contracts;

/// <summary>
/// Sign-in with an unknown email or a wrong password (401 UNAUTHORIZED). One
/// message for both, so a caller cannot learn which emails have an account.
/// </summary>
public sealed class InvalidCredentialsException()
    : Exception("The email or password is not correct.");
