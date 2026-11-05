using BudTack.Identity;
using BudTack.Identity.Internal.Application;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

public sealed class TokenServiceTests
{
    private static readonly IdentitySettings Settings = TestModules.Identity;
    private static readonly Guid UserId = Guid.Parse("0193a1b2-0000-7000-8000-000000000001");

    [Fact]
    public async Task ValidateAsync_freshToken_returnsItsUser()
    {
        var tokens = new TokenService(Settings, TimeProvider.System);

        Assert.Equal(UserId, await tokens.ValidateAsync(tokens.Issue(UserId)));
    }

    [Fact]
    public async Task ValidateAsync_expiredToken_isRefused()
    {
        var issuedTwoHoursAgo = new TokenService(Settings, new FixedClock(DateTimeOffset.UtcNow.AddHours(-2)));
        var token = issuedTwoHoursAgo.Issue(UserId);

        Assert.Null(await new TokenService(Settings, TimeProvider.System).ValidateAsync(token));
    }

    [Fact]
    public async Task ValidateAsync_tokenSignedWithAnotherKey_isRefused()
    {
        var other = new IdentitySettings("another-secret-another-secret-01234567", TimeSpan.FromHours(1));
        var token = new TokenService(other, TimeProvider.System).Issue(UserId);

        Assert.Null(await new TokenService(Settings, TimeProvider.System).ValidateAsync(token));
    }

    [Fact]
    public async Task ValidateAsync_tamperedPayload_isRefused()
    {
        var tokens = new TokenService(Settings, TimeProvider.System);
        var parts = tokens.Issue(UserId).Split('.');
        var forged = string.Join('.', parts[0], parts[1][..^2] + (parts[1][^2] == 'A' ? "BA" : "AA"), parts[2]);

        Assert.Null(await tokens.ValidateAsync(forged));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("a.b.c")]
    public async Task ValidateAsync_garbage_isRefused(string token)
    {
        Assert.Null(await new TokenService(Settings, TimeProvider.System).ValidateAsync(token));
    }

    [Fact]
    public async Task ValidateAsync_unsignedToken_isRefused()
    {
        // {"alg":"none"} with our claims: must never pass.
        const string unsigned =
            "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0." +
            "eyJzdWIiOiIwMTkzYTFiMi0wMDAwLTcwMDAtODAwMC0wMDAwMDAwMDAwMDEiLCJpc3MiOiJidWQtdGFjay1hcGkiLCJhdWQiOiJidWQtdGFjayIsImV4cCI6NDEwMjQ0NDgwMH0.";

        Assert.Null(await new TokenService(Settings, TimeProvider.System).ValidateAsync(unsigned));
    }

    [Fact]
    public void Settings_shortSecret_isRefused()
    {
        Assert.Throws<ArgumentException>(() => new IdentitySettings("too-short", TimeSpan.FromHours(1)));
    }

    [Fact]
    public void Settings_zeroLifetime_isRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new IdentitySettings("unit-test-secret-unit-test-secret-0123", TimeSpan.Zero));
    }

    [Fact]
    public void Settings_toString_neverShowsTheSecret()
    {
        Assert.DoesNotContain(Settings.JwtSecret, Settings.ToString(), StringComparison.Ordinal);
    }
}
