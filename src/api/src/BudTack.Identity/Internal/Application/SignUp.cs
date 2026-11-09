using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Domain;
using Microsoft.Extensions.Logging;

namespace BudTack.Identity.Internal.Application;

/// <summary>
/// HU-01, sign-up: validate, refuse a taken email (TC-001), store only the
/// password's hash (TC-014). The password never reaches a log line, and a new
/// account is always a <c>USER</c> (HU-09).
/// </summary>
internal sealed partial class SignUp(IUsersRepository users, PasswordHasher hasher, ILogger<SignUp> logger)
{
    public async Task<Api.Contracts.User> HandleAsync(NewUser request, CancellationToken cancellationToken)
    {
        var (name, email, password) = SignUpRules.Validate(request);

        // A friendly early answer; the unique index still decides a race.
        if (await users.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false) is not null)
        {
            throw new EmailAlreadyInUseException();
        }

        var user = new Domain.User(Guid.CreateVersion7(), name, email, hasher.Hash(password), Roles.User);
        await users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        LogSignedUp(logger, user.Id);

        return new Api.Contracts.User(user.Id, user.Name, user.Email, user.Role);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Account {UserId} signed up")]
    private static partial void LogSignedUp(ILogger logger, Guid userId);
}
