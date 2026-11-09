using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Application;

namespace BudTack.Identity.Api;

/// <summary>
/// The only entry point into the identity module (09-modules/identity):
/// the accounts an administrator creates, their single-use links, sign-in, and
/// checking the bearer token every other request carries.
/// </summary>
public sealed class IdentityFacade
{
    private readonly CreateUser _createUser;
    private readonly AccountLinks _links;
    private readonly SignIn _signIn;
    private readonly TokenService _tokens;
    private readonly ListUsers _listUsers;
    private readonly ManageAccount _manageAccount;
    private readonly IUsersRepository _users;

    internal IdentityFacade(CreateUser createUser, AccountLinks links, SignIn signIn, TokenService tokens, ListUsers listUsers, ManageAccount manageAccount, IUsersRepository users)
    {
        _createUser = createUser;
        _links = links;
        _signIn = signIn;
        _tokens = tokens;
        _listUsers = listUsers;
        _manageAccount = manageAccount;
        _users = users;
    }

    /// <summary>
    /// <c>POST /users</c>, for the role <c>ADMIN</c> only; the host checks the role.
    /// Throws <see cref="IdentityValidationException"/> or <see cref="EmailAlreadyInUseException"/>.
    /// </summary>
    public Task<CreatedUser> CreateUserAsync(Guid actor, NewUser request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _createUser.HandleAsync(actor, request, cancellationToken);
    }

    /// <summary><c>POST /users/{id}/activation-links</c>; cancels the account's earlier links.</summary>
    public Task<AccountLink> CreateActivationLinkAsync(Guid actor, Guid userId, CancellationToken cancellationToken = default) =>
        _links.CreateActivationAsync(actor, userId, cancellationToken);

    /// <summary><c>POST /users/{id}/password-resets</c>; cancels the account's earlier links.</summary>
    public Task<AccountLink> CreatePasswordResetAsync(Guid actor, Guid userId, CancellationToken cancellationToken = default) =>
        _links.CreatePasswordResetAsync(actor, userId, cancellationToken);

    /// <summary><c>POST /account-links/confirm</c>. Throws <see cref="IdentityValidationException"/>.</summary>
    public Task ConfirmLinkAsync(LinkConfirmation request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _links.ConfirmAsync(request, cancellationToken);
    }

    /// <summary>
    /// <c>POST /sessions</c>. Throws <see cref="IdentityValidationException"/> or
    /// <see cref="InvalidCredentialsException"/>.
    /// </summary>
    public Task<Session> SignInAsync(Credentials request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _signIn.HandleAsync(request, cancellationToken);
    }

    /// <summary>
    /// <c>GET /users</c>, for the role <c>ADMIN</c> only; the host checks the role
    /// (HU-09). Throws <see cref="IdentityValidationException"/>.
    /// </summary>
    public Task<UserPage> ListUsersAsync(int? page, int? limit, CancellationToken cancellationToken = default) =>
        _listUsers.HandleAsync(page, limit, cancellationToken);

    /// <summary>The names of these accounts, for another module to show who did something.</summary>
    public Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
        _users.NamesOfAsync(userIds, cancellationToken);

    /// <summary><c>PATCH /users/{id}</c>, for the role <c>ADMIN</c> only (BR-09).</summary>
    public Task<User> SetUserActiveAsync(
        Guid actor, Guid userId, UserStatusUpdate request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _manageAccount.SetActiveAsync(actor, userId, request, cancellationToken);
    }

    /// <summary><c>DELETE /users/{id}</c>, for the role <c>ADMIN</c> only; only a never-activated account (BR-09).</summary>
    public Task DeleteUserAsync(Guid actor, Guid userId, CancellationToken cancellationToken = default) =>
        _manageAccount.DeleteAsync(actor, userId, cancellationToken);

    /// <summary>
    /// Who a bearer token stands for, or null when it is not one of ours, has
    /// expired, or its account is no longer active (ADR-011).
    /// </summary>
    public async Task<AuthenticatedUser?> AuthenticateAsync(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        var user = await _tokens.ValidateAsync(token).ConfigureAwait(false);
        return user is not null && await _users.IsActiveAsync(user.Id, CancellationToken.None).ConfigureAwait(false) ? user : null;
    }
}
