using BudTack.Identity.Api.Contracts;
using Microsoft.Extensions.Logging;

namespace BudTack.Identity.Internal.Application;

/// <summary>
/// HU-13, BR-09: an administrator inactivates or reactivates an account, and
/// deletes one that was never activated. Nobody can inactivate themselves, so the
/// one acting stays active and the application always keeps an administrator.
/// </summary>
internal sealed partial class ManageAccount(IUsersRepository users, ILogger<ManageAccount> logger)
{
    public async Task<Api.Contracts.User> SetActiveAsync(
        Guid actor, Guid userId, UserStatusUpdate request, CancellationToken cancellationToken)
    {
        var isActive = request.IsActive
            ?? throw new IdentityValidationException([new FieldError("isActive", "is required: true or false")]);

        if (!isActive && actor == userId)
        {
            throw new AccountStateConflictException("You cannot inactivate your own account.");
        }

        var user = await users.SetActiveAsync(actor, userId, isActive, cancellationToken).ConfigureAwait(false)
            ?? throw new UserNotFoundException();
        LogStatus(logger, userId, isActive, actor);
        return new Api.Contracts.User(user.Id, user.Name, user.Email, user.Role, user.Status);
    }

    public async Task DeleteAsync(Guid actor, Guid userId, CancellationToken cancellationToken)
    {
        if (await users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false) is null)
        {
            throw new UserNotFoundException();
        }

        if (!await users.DeleteNeverActivatedAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            throw new AccountStateConflictException("The account was activated; inactivate it instead.");
        }

        LogDeleted(logger, userId, actor);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Account {UserId} active: {IsActive}, by {Actor}")]
    private static partial void LogStatus(ILogger logger, Guid userId, bool isActive, Guid actor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Account {UserId} deleted by {Actor}")]
    private static partial void LogDeleted(ILogger logger, Guid userId, Guid actor);
}
