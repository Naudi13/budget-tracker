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

    internal IdentityFacade(CreateUser createUser, AccountLinks links, SignIn signIn, TokenService tokens, ListUsers listUsers)
    {
        _createUser = createUser;
        _links = links;
        _signIn = signIn;
        _tokens = tokens;
        _listUsers = listUsers;
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

    /// <summary>Who a bearer token stands for, or null when it is not one of ours or has expired.</summary>
    public Task<AuthenticatedUser?> AuthenticateAsync(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _tokens.ValidateAsync(token);
    }
}
