using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Application;

namespace BudTack.Identity.Api;

/// <summary>
/// The only entry point into the identity module (09-modules/identity):
/// sign-up, sign-in, and checking the bearer token every other request carries.
/// </summary>
public sealed class IdentityFacade
{
    private readonly SignUp _signUp;
    private readonly SignIn _signIn;
    private readonly TokenService _tokens;

    internal IdentityFacade(SignUp signUp, SignIn signIn, TokenService tokens)
    {
        _signUp = signUp;
        _signIn = signIn;
        _tokens = tokens;
    }

    /// <summary>
    /// <c>POST /users</c>. Throws <see cref="IdentityValidationException"/> or
    /// <see cref="EmailAlreadyInUseException"/>.
    /// </summary>
    public Task<User> SignUpAsync(NewUser request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _signUp.HandleAsync(request, cancellationToken);
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

    /// <summary>The user id a bearer token stands for, or null when it is not one of ours or has expired.</summary>
    public Task<Guid?> AuthenticateAsync(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _tokens.ValidateAsync(token);
    }
}
