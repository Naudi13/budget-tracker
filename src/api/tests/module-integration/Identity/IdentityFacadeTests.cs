using BudTack.Identity;
using BudTack.Identity.Api;
using BudTack.Identity.Api.Contracts;
using BudTack.ModuleIntegration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace BudTack.ModuleIntegration.Tests.Identity;

/// <summary>
/// Accounts through <see cref="IdentityFacade"/> only, against a real PostgreSQL:
/// the unique index, the stored hashes and the single statement that spends a link.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class IdentityFacadeTests(PostgresFixture database) : IAsyncLifetime, IDisposable
{
    private const string Password = "Str0ngPass!23";

    private static readonly Guid Admin = Guid.Parse("0193a1b2-0000-7000-8000-0000000000ad");

    private static readonly IdentitySettings Settings =
        new("module-integration-secret-0123456789abc", TimeSpan.FromHours(1));

    private ServiceProvider _services = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await database.ResetAsync();
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(database.DataSource)
            .AddIdentityModule(Settings)
            .BuildServiceProvider();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Dispose() => _services.Dispose();

    private IdentityFacade Facade() => _services.CreateScope().ServiceProvider.GetRequiredService<IdentityFacade>();

    private Task<CreatedUser> Create(string email = "owner@example.com", string role = Roles.User) =>
        Facade().CreateUserAsync(Admin, new NewUser("Ana Owner", email, role), Ct);

    [Fact]
    public async Task CreateUserAsync_isStoredPending_withWhoCreatedIt() // TC-022
    {
        var created = await Create("Owner@Example.com");

        await using var command = database.DataSource.CreateCommand(
            "SELECT email, password_hash, created_by FROM identity.users WHERE id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = created.User.Id });
        await using var row = await command.ExecuteReaderAsync(Ct);
        Assert.True(await row.ReadAsync(Ct));
        Assert.Equal("owner@example.com", row.GetString(0));
        Assert.True(row.IsDBNull(1));
        Assert.Equal(Admin, row.GetGuid(2));
        Assert.Equal(AccountStatus.Pending, created.User.Status);
    }

    [Fact]
    public async Task CreateUserAsync_emailInUse_isRefusedAndAddsNoRow() // TC-001
    {
        await Create();

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(() => Create());
        Assert.Equal(1L, await CountUsersAsync());
    }

    [Fact]
    public async Task CreateUserAsync_twoAtOnceWithOneEmail_storesOneAndRefusesTheOther()
    {
        var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            try
            {
                await Create("race@example.com");
                return "created";
            }
            catch (EmailAlreadyInUseException)
            {
                return "conflict";
            }
        }));

        Assert.Equal(["conflict", "created"], attempts.Order());
        Assert.Equal(1L, await CountUsersAsync());
    }

    [Fact]
    public async Task ConfirmLinkAsync_storesABcryptHash_andNeverTheToken() // TC-014, TC-023
    {
        var created = await Create();

        await Facade().ConfirmLinkAsync(new LinkConfirmation(created.Activation.Token, Password), Ct);

        await using var command = database.DataSource.CreateCommand(
            "SELECT u.password_hash, t.token_hash, t.used_at IS NOT NULL FROM identity.users u JOIN identity.account_tokens t ON t.user_id = u.id");
        await using var row = await command.ExecuteReaderAsync(Ct);
        Assert.True(await row.ReadAsync(Ct));
        Assert.Matches(@"^\$2a\$12\$", row.GetString(0));
        Assert.NotEqual(created.Activation.Token, row.GetString(1));
        Assert.True(row.GetBoolean(2));
    }

    [Fact]
    public async Task ConfirmLinkAsync_twoAtOnce_onlyOneWins() // TC-023
    {
        var created = await Create();

        var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(async i =>
        {
            try
            {
                await Facade().ConfirmLinkAsync(new LinkConfirmation(created.Activation.Token, Password + i), Ct);
                return "used";
            }
            catch (IdentityValidationException)
            {
                return "refused";
            }
        }));

        Assert.Equal(["refused", "used"], attempts.Order());
    }

    [Fact]
    public async Task SignInAsync_thenAuthenticateAsync_returnsTheAccount()
    {
        var created = await Create();
        await Facade().ConfirmLinkAsync(new LinkConfirmation(created.Activation.Token, Password), Ct);

        var session = await Facade().SignInAsync(new Credentials("OWNER@example.com", Password), Ct);

        Assert.Equal(new AuthenticatedUser(created.User.Id, Roles.User), await Facade().AuthenticateAsync(session.Token));
        Assert.Equal(Roles.User, session.Role);
    }

    [Fact]
    public async Task SignInAsync_ofAPendingAccount_isRefused()
    {
        await Create();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => Facade().SignInAsync(new Credentials("owner@example.com", Password), Ct));
    }

    [Fact]
    public async Task CreatePasswordResetAsync_replacesThePassword_andCancelsTheEarlierLink() // TC-023
    {
        var created = await Create();
        await Facade().ConfirmLinkAsync(new LinkConfirmation(created.Activation.Token, Password), Ct);
        var first = await Facade().CreatePasswordResetAsync(Admin, created.User.Id, Ct);
        var second = await Facade().CreatePasswordResetAsync(Admin, created.User.Id, Ct);

        await Assert.ThrowsAsync<IdentityValidationException>(
            () => Facade().ConfirmLinkAsync(new LinkConfirmation(first.Token, "An0therPass!"), Ct));
        await Facade().ConfirmLinkAsync(new LinkConfirmation(second.Token, "An0therPass!"), Ct);

        await Facade().SignInAsync(new Credentials("owner@example.com", "An0therPass!"), Ct);
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => Facade().SignInAsync(new Credentials("owner@example.com", Password), Ct));
    }

    [Fact]
    public async Task ListUsersAsync_readsTheRoleAndState() // HU-09
    {
        await Create("zoe@example.com");
        await Create("admin@example.com", Roles.Admin);

        var page = await Facade().ListUsersAsync(null, null, Ct);

        Assert.Equal(["admin@example.com", "zoe@example.com"], page.Data.Select(user => user.Email));
        Assert.Equal([Roles.Admin, Roles.User], page.Data.Select(user => user.Role));
        Assert.All(page.Data, user => Assert.Equal(AccountStatus.Pending, user.Status));
        Assert.Equal(2L, page.Meta.Total);
    }

    private async Task<long> CountUsersAsync()
    {
        await using var command = database.DataSource.CreateCommand("SELECT count(*) FROM identity.users");
        return (long)(await command.ExecuteScalarAsync(Ct))!;
    }
}
