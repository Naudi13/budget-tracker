using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

/// <summary>The HU-01 use cases against identity.users in memory.</summary>
public sealed class SignUpAndSignInTests
{
    private readonly InMemoryUsers _users = new();
    private readonly PasswordHasher _hasher = new(iterations: 1_000);
    private readonly TokenService _tokens = new(TestModules.Identity, TimeProvider.System);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SignUp SignUp => new(_users, _hasher, NullLogger<SignUp>.Instance);

    private SignIn SignIn => new(_users, _hasher, _tokens, NullLogger<SignIn>.Instance);

    [Fact]
    public async Task SignUp_newEmail_storesTheAccountWithAHashedPassword()
    {
        var user = await SignUp.HandleAsync(new NewUser("Ana Owner", "Owner@Example.com", "Str0ngPass!23"), Ct);

        var row = Assert.Single(_users.Rows);
        Assert.Equal(new User(row.Id, "Ana Owner", "owner@example.com"), user);
        Assert.NotEqual("Str0ngPass!23", row.PasswordHash);
        Assert.True(PasswordHasher.Verify("Str0ngPass!23", row.PasswordHash));
    }

    [Fact]
    public async Task SignUp_emailAlreadyInUse_isRefusedAndStoresNothing() // TC-001
    {
        await SignUp.HandleAsync(new NewUser("Ana Owner", "owner@example.com", "Str0ngPass!23"), Ct);

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(
            () => SignUp.HandleAsync(new NewUser("New Owner", "OWNER@example.com", "An0therPass!"), Ct));
        Assert.Single(_users.Rows);
    }

    [Fact]
    public async Task SignUp_invalidRequest_storesNothing()
    {
        await Assert.ThrowsAsync<IdentityValidationException>(
            () => SignUp.HandleAsync(new NewUser("", "owner@example.com", "Str0ngPass!23"), Ct));
        Assert.Empty(_users.Rows);
    }

    [Fact]
    public async Task SignIn_rightPassword_returnsATokenForThatUser()
    {
        var user = await SignUp.HandleAsync(new NewUser("Ana Owner", "owner@example.com", "Str0ngPass!23"), Ct);

        var session = await SignIn.HandleAsync(new Credentials(" OWNER@example.com", "Str0ngPass!23"), Ct);

        Assert.Equal("Bearer", session.TokenType);
        Assert.Equal(3600, session.ExpiresIn);
        Assert.Equal(user.Id, await _tokens.ValidateAsync(session.Token));
    }

    [Fact]
    public async Task SignIn_wrongPassword_isRefused()
    {
        await SignUp.HandleAsync(new NewUser("Ana Owner", "owner@example.com", "Str0ngPass!23"), Ct);

        var error = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => SignIn.HandleAsync(new Credentials("owner@example.com", "wrong-password"), Ct));
        Assert.Equal("The email or password is not correct.", error.Message);
    }

    [Fact]
    public async Task SignIn_unknownEmail_failsExactlyLikeAWrongPassword()
    {
        var error = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => SignIn.HandleAsync(new Credentials("nobody@example.com", "Str0ngPass!23"), Ct));
        Assert.Equal("The email or password is not correct.", error.Message);
    }

    [Fact]
    public async Task SignIn_missingFields_isAValidationError()
    {
        await Assert.ThrowsAsync<IdentityValidationException>(
            () => SignIn.HandleAsync(new Credentials("owner@example.com", null), Ct));
    }
}
