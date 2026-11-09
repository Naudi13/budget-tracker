using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Domain;
using Microsoft.Extensions.Logging;

namespace BudTack.Identity.Internal.Application;

/// <summary>
/// HU-09: an administrator creates an account with no password (BR-07, TC-001,
/// TC-022) and gets the link its owner activates it with.
/// </summary>
internal sealed partial class CreateUser(IUsersRepository users, AccountLinks links, ILogger<CreateUser> logger)
{
    public async Task<CreatedUser> HandleAsync(Guid actor, NewUser request, CancellationToken cancellationToken)
    {
        var (name, email, role) = AccountRules.Validate(request);

        // A friendly early answer; the unique index still decides a race.
        if (await users.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false) is not null)
        {
            throw new EmailAlreadyInUseException();
        }

        var user = new Domain.User(Guid.CreateVersion7(), name, email, PasswordHash: null, role);
        await users.AddAsync(user, actor, cancellationToken).ConfigureAwait(false);
        LogCreated(logger, user.Id, actor);

        var activation = await links.IssueActivationAsync(actor, user.Id, cancellationToken).ConfigureAwait(false);
        return new CreatedUser(new Api.Contracts.User(user.Id, user.Name, user.Email, user.Role, user.Status), activation);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Account {UserId} created by {Actor}")]
    private static partial void LogCreated(ILogger logger, Guid userId, Guid actor);
}
