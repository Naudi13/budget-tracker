using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

/// <summary>HU-01, HU-09 and HU-10 against the tables in memory.</summary>
public sealed class AccountsTests
{
    private const string Password = "Str0ngPass!23";

    private static readonly Guid Admin = Guid.Parse("0193a1b2-0000-7000-8000-0000000000ad");
    private static readonly DateTimeOffset Noon = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryUsers _users = new();
    private readonly InMemoryAccountTokens _tokens;
    private readonly PasswordHasher _hasher = new(workFactor: 4);
    private readonly TokenService _sessions = new(TestModules.Identity, TimeProvider.System);
    private readonly FixedClock _clock = new(Noon);
    private readonly AccountLinks _links;
    private readonly CreateUser _create;

    public AccountsTests()
    {
        _tokens = new InMemoryAccountTokens(_users);
        _links = new AccountLinks(_users, _tokens, _hasher, _clock, NullLogger<AccountLinks>.Instance);
        _create = new CreateUser(_users, _links, NullLogger<CreateUser>.Instance);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SignIn SignIn => new(_users, _hasher, _sessions, NullLogger<SignIn>.Instance);

    private Task<CreatedUser> Create(string email = "owner@example.com", string role = Roles.User) =>
        _create.HandleAsync(Admin, new NewUser("Ana Owner", email, role), Ct);

    private async Task<CreatedUser> Active(string email = "owner@example.com")
    {
        var created = await Create(email);
        await _links.ConfirmAsync(new LinkConfirmation(created.Activation.Token, Password), Ct);
        return created;
    }

    private Task<Session> Login(string password, string email = "owner@example.com") =>
        SignIn.HandleAsync(new Credentials(email, password), Ct);

    [Fact]
    public async Task Create_anAccount_isPending_withNoPassword_andAnActivationLink() // TC-022
    {
        var created = await Create("Owner@Example.com");

        var row = Assert.Single(_users.Rows);
        Assert.Equal(new User(row.Id, "Ana Owner", "owner@example.com", Roles.User, AccountStatus.Pending), created.User);
        Assert.Null(row.PasswordHash);
        Assert.Equal(Noon + AccountLinks.ActivationLifetime, created.Activation.ExpiresAt);
    }

    [Fact]
    public async Task Create_storesOnlyTheHashOfTheToken()
    {
        var created = await Create();

        var token = Assert.Single(_tokens.Rows);
        Assert.NotEqual(created.Activation.Token, token.TokenHash);
        Assert.Equal(AccountLinks.HashOf(created.Activation.Token), token.TokenHash);
    }

    [Fact]
    public async Task Create_anEmailAlreadyInUse_isRefused_andStoresNothing() // TC-001
    {
        await Create();

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(() => Create("OWNER@example.com"));
        Assert.Single(_users.Rows);
    }

    [Fact]
    public async Task Create_anInvalidRequest_storesNothing()
    {
        await Assert.ThrowsAsync<IdentityValidationException>(() => Create(role: "OWNER"));
        Assert.Empty(_users.Rows);
    }

    [Fact]
    public async Task Confirm_activatesTheAccount_withItsOwnPassword() // TC-014, TC-023
    {
        await Active();

        var row = Assert.Single(_users.Rows);
        Assert.Equal(AccountStatus.Active, row.Status);
        Assert.NotEqual(Password, row.PasswordHash);
        Assert.Equal(Roles.User, (await Login(Password)).Role);
    }

    [Fact]
    public async Task Confirm_twice_isRefused_likeAnyInvalidLink() // TC-023
    {
        var created = await Create();
        await _links.ConfirmAsync(new LinkConfirmation(created.Activation.Token, Password), Ct);

        var used = await Assert.ThrowsAsync<IdentityValidationException>(
            () => _links.ConfirmAsync(new LinkConfirmation(created.Activation.Token, "An0therPass!"), Ct));
        var unknown = await Assert.ThrowsAsync<IdentityValidationException>(
            () => _links.ConfirmAsync(new LinkConfirmation("not-a-token", Password), Ct));

        Assert.Equal(used.Errors, unknown.Errors);
        Assert.Equal("token", Assert.Single(used.Errors).Field);
    }

    [Fact]
    public async Task Confirm_afterItExpires_isRefused() // TC-023
    {
        var created = await Create();
        _clock.Advance(AccountLinks.ActivationLifetime);

        await Assert.ThrowsAsync<IdentityValidationException>(
            () => _links.ConfirmAsync(new LinkConfirmation(created.Activation.Token, Password), Ct));
    }

    [Fact]
    public async Task CreateActivation_cancelsTheEarlierLink() // TC-022
    {
        var created = await Create();

        var second = await _links.CreateActivationAsync(Admin, created.User.Id, Ct);

        await Assert.ThrowsAsync<IdentityValidationException>(
            () => _links.ConfirmAsync(new LinkConfirmation(created.Activation.Token, Password), Ct));
        await _links.ConfirmAsync(new LinkConfirmation(second.Token, Password), Ct);
    }

    [Fact]
    public async Task CreateActivation_ofAnActiveAccount_isAConflict()
    {
        var created = await Active();

        await Assert.ThrowsAsync<AccountStateConflictException>(() => _links.CreateActivationAsync(Admin, created.User.Id, Ct));
    }

    [Fact]
    public async Task CreatePasswordReset_ofAPendingAccount_isAConflict()
    {
        var created = await Create();

        await Assert.ThrowsAsync<AccountStateConflictException>(() => _links.CreatePasswordResetAsync(Admin, created.User.Id, Ct));
    }

    [Fact]
    public async Task CreatePasswordReset_ofAnUnknownAccount_isNotFound()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(() => _links.CreatePasswordResetAsync(Admin, Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task PasswordReset_changesThePassword_andExpiresIn30Minutes() // TC-023
    {
        var created = await Active();

        var link = await _links.CreatePasswordResetAsync(Admin, created.User.Id, Ct);
        await _links.ConfirmAsync(new LinkConfirmation(link.Token, "An0therPass!"), Ct);

        Assert.Equal(Noon + AccountLinks.ResetLifetime, link.ExpiresAt);
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => Login(Password));
        await Login("An0therPass!");
    }

    [Fact]
    public async Task SignIn_rightPassword_returnsATokenForThatUser()
    {
        var created = await Active();

        var session = await SignIn.HandleAsync(new Credentials(" OWNER@example.com", Password), Ct);

        Assert.Equal("Bearer", session.TokenType);
        Assert.Equal(3600, session.ExpiresIn);
        Assert.Equal(new AuthenticatedUser(created.User.Id, Roles.User), await _sessions.ValidateAsync(session.Token));
    }

    [Fact]
    public async Task SignIn_pendingWrongOrUnknown_allFailTheSameWay()
    {
        await Create("pending@example.com");
        await Active();

        var pending = await Assert.ThrowsAsync<InvalidCredentialsException>(() => Login(Password, "pending@example.com"));
        var wrong = await Assert.ThrowsAsync<InvalidCredentialsException>(() => Login("wrong-password"));
        var unknown = await Assert.ThrowsAsync<InvalidCredentialsException>(() => Login(Password, "nobody@example.com"));

        Assert.Equal("The email or password is not correct.", pending.Message);
        Assert.Equal(pending.Message, wrong.Message);
        Assert.Equal(pending.Message, unknown.Message);
    }

    [Fact]
    public async Task SignIn_missingFields_isAValidationError()
    {
        await Assert.ThrowsAsync<IdentityValidationException>(
            () => SignIn.HandleAsync(new Credentials("owner@example.com", null), Ct));
    }

    private ManageAccount Manage => new(_users, NullLogger<ManageAccount>.Instance);

    [Fact]
    public async Task SetActive_false_blocksSignIn_andTrueRestoresIt() // TC-027
    {
        var created = await Active();

        var off = await Manage.SetActiveAsync(Admin, created.User.Id, new UserStatusUpdate(false), Ct);

        Assert.Equal(AccountStatus.Inactive, off.Status);
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => Login(Password));
        var on = await Manage.SetActiveAsync(Admin, created.User.Id, new UserStatusUpdate(true), Ct);
        Assert.Equal(AccountStatus.Active, on.Status);
        await Login(Password);
    }

    [Fact]
    public async Task SetActive_onYourself_isAConflict() // BR-09
    {
        var created = await Active();

        await Assert.ThrowsAsync<AccountStateConflictException>(
            () => Manage.SetActiveAsync(created.User.Id, created.User.Id, new UserStatusUpdate(false), Ct));
    }

    [Fact]
    public async Task SetActive_withoutIsActive_isAValidationError()
    {
        var created = await Active();

        await Assert.ThrowsAsync<IdentityValidationException>(
            () => Manage.SetActiveAsync(Admin, created.User.Id, new UserStatusUpdate(null), Ct));
    }

    [Fact]
    public async Task SetActive_ofAnUnknownAccount_isNotFound()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => Manage.SetActiveAsync(Admin, Guid.NewGuid(), new UserStatusUpdate(false), Ct));
    }

    [Fact]
    public async Task Delete_aNeverActivatedAccount_removesIt_andAnActivatedOne_isAConflict() // TC-027
    {
        var pending = await Create("pending@example.com");
        var active = await Active();

        await Manage.DeleteAsync(Admin, pending.User.Id, Ct);

        Assert.Equal(active.User.Id, Assert.Single(_users.Rows).Id);
        await Assert.ThrowsAsync<AccountStateConflictException>(() => Manage.DeleteAsync(Admin, active.User.Id, Ct));
        await Assert.ThrowsAsync<UserNotFoundException>(() => Manage.DeleteAsync(Admin, pending.User.Id, Ct));
    }

    [Fact]
    public async Task Links_ofAnInactiveAccount_areAConflict()
    {
        var created = await Active();
        await Manage.SetActiveAsync(Admin, created.User.Id, new UserStatusUpdate(false), Ct);

        await Assert.ThrowsAsync<AccountStateConflictException>(() => _links.CreatePasswordResetAsync(Admin, created.User.Id, Ct));
        await Assert.ThrowsAsync<AccountStateConflictException>(() => _links.CreateActivationAsync(Admin, created.User.Id, Ct));
    }
}
