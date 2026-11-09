namespace BudTack.Identity.Api.Contracts;

/// <summary>
/// Sign-in refused after too many failures with one email (429, RNF-02).
/// <see cref="RetryAfter"/> is how long until that email may try again.
/// </summary>
public sealed class TooManySignInAttemptsException(TimeSpan retryAfter)
    : Exception("Too many failed sign-ins for this email. Try again later.")
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
