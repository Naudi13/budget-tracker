using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Domain;
using Microsoft.Extensions.Logging;

namespace BudTack.Identity.Internal.Application;

/// <summary>
/// HU-01, sign-in: a valid email and password get a bearer token. An unknown
/// email, a pending or inactive account and a wrong password fail the same way and take the same time, so a
/// caller cannot learn which emails have an account.
/// </summary>
internal sealed partial class SignIn(
    IUsersRepository users, PasswordHasher hasher, TokenService tokens, ILogger<SignIn> logger)
{
    // Verified against when there is no hash to check, so every failure costs one hash.
    private readonly Lazy<string> _decoy = new(() => hasher.Hash(Guid.NewGuid().ToString()));

    public async Task<Session> HandleAsync(Credentials request, CancellationToken cancellationToken)
    {
        var (email, password) = AccountRules.Validate(request);

        var user = await users.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        var matches = PasswordHasher.Verify(password, user?.PasswordHash ?? _decoy.Value);
        if (user is not { IsActive: true, PasswordHash: not null } || !matches)
        {
            LogRefused(logger);
            throw new InvalidCredentialsException();
        }

        LogSignedIn(logger, user.Id);
        return new Session(tokens.Issue(user.Id, user.Role), "Bearer", (int)tokens.Lifetime.TotalSeconds, user.Role);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Account {UserId} signed in")]
    private static partial void LogSignedIn(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A sign-in was refused: wrong email or password")]
    private static partial void LogRefused(ILogger logger);
}
