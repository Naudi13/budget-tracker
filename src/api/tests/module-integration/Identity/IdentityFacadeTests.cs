using BudTack.Identity;
using BudTack.Identity.Api;
using BudTack.Identity.Api.Contracts;
using BudTack.ModuleIntegration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace BudTack.ModuleIntegration.Tests.Identity;

/// <summary>
/// HU-01 through <see cref="IdentityFacade"/> only, against identity.users in a
/// real PostgreSQL: the unique index, the stored hash and the token round trip.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class IdentityFacadeTests(PostgresFixture database) : IAsyncLifetime, IDisposable
{
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

    [Fact]
    public async Task SignUpAsync_newAccount_isStoredWithItsPasswordOnlyAsAHash() // TC-014
    {
        var user = await Facade().SignUpAsync(new NewUser("Ana Owner", "Owner@Example.com", "Str0ngPass!23"), Ct);

        await using var command = database.DataSource.CreateCommand(
            "SELECT name, email, password_hash, created_by FROM identity.users WHERE id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = user.Id });
        await using var row = await command.ExecuteReaderAsync(Ct);
        Assert.True(await row.ReadAsync(Ct));
        Assert.Equal("Ana Owner", row.GetString(0));
        Assert.Equal("owner@example.com", row.GetString(1));
        Assert.NotEqual("Str0ngPass!23", row.GetString(2));
        Assert.Matches(@"^pbkdf2-sha256\$600000\$", row.GetString(2));
        Assert.Equal(user.Id, row.GetGuid(3));
    }

    [Fact]
    public async Task SignUpAsync_emailInUse_isRefusedAndAddsNoRow() // TC-001
    {
        await Facade().SignUpAsync(new NewUser("Ana Owner", "owner@example.com", "Str0ngPass!23"), Ct);

        await Assert.ThrowsAsync<EmailAlreadyInUseException>(
            () => Facade().SignUpAsync(new NewUser("New Owner", "owner@example.com", "An0therPass!"), Ct));
        Assert.Equal(1L, await CountUsersAsync());
    }

    [Fact]
    public async Task SignUpAsync_twoAtOnceWithOneEmail_storesOneAndRefusesTheOther()
    {
        var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(async i =>
        {
            try
            {
                await Facade().SignUpAsync(new NewUser($"Owner {i}", "race@example.com", "Str0ngPass!23"), Ct);
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
    public async Task SignInAsync_thenAuthenticateAsync_returnsTheSignedUpUser()
    {
        var user = await Facade().SignUpAsync(new NewUser("Ana Owner", "owner@example.com", "Str0ngPass!23"), Ct);

        var session = await Facade().SignInAsync(new Credentials("OWNER@example.com", "Str0ngPass!23"), Ct);

        Assert.Equal(user.Id, await Facade().AuthenticateAsync(session.Token));
    }

    [Fact]
    public async Task SignInAsync_wrongPassword_isRefused()
    {
        await Facade().SignUpAsync(new NewUser("Ana Owner", "owner@example.com", "Str0ngPass!23"), Ct);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => Facade().SignInAsync(new Credentials("owner@example.com", "Wrong-Pass-1"), Ct));
    }

    private async Task<long> CountUsersAsync()
    {
        await using var command = database.DataSource.CreateCommand("SELECT count(*) FROM identity.users");
        return (long)(await command.ExecuteScalarAsync(Ct))!;
    }
}
