using BudTack.Api.Platform.Config;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BudTack.Unit.Tests.Platform;

public sealed class IdentityConfigurationTests
{
    private const string Secret = "unit-test-secret-unit-test-secret-0123";

    private static IConfiguration Config(string? secret, string? expiry) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AUTH_JWT_SECRET"] = secret,
                ["AUTH_JWT_EXPIRY_SECONDS"] = expiry,
            })
            .Build();

    [Fact]
    public void Read_secretAndExpiry_usesBoth()
    {
        var settings = IdentityConfiguration.Read(Config(Secret, "900"));

        Assert.Equal(Secret, settings.JwtSecret);
        Assert.Equal(TimeSpan.FromSeconds(900), settings.TokenLifetime);
    }

    [Fact]
    public void Read_noExpiry_defaultsToOneHour()
    {
        Assert.Equal(TimeSpan.FromHours(1), IdentityConfiguration.Read(Config(Secret, null)).TokenLifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Read_noSecret_stopsTheHost(string? secret)
    {
        var error = Assert.Throws<InvalidOperationException>(() => IdentityConfiguration.Read(Config(secret, null)));
        Assert.Contains("AUTH_JWT_SECRET is not set", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_shortSecret_stopsTheHost()
    {
        var error = Assert.Throws<InvalidOperationException>(() => IdentityConfiguration.Read(Config("short", null)));
        Assert.Contains("at least 32 bytes", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("an hour")]
    [InlineData("1.5")]
    public void Read_badExpiry_stopsTheHost(string expiry)
    {
        Assert.Throws<InvalidOperationException>(() => IdentityConfiguration.Read(Config(Secret, expiry)));
    }
}
