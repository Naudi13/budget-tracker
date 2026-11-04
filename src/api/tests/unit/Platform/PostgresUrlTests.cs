using BudTack.Api.Platform.Config;
using Npgsql;
using Xunit;

namespace BudTack.Unit.Tests.Platform;

public sealed class PostgresUrlTests
{
    [Fact]
    public void ToConnectionString_withFullUrl_mapsEveryPart()
    {
        var connectionString = PostgresUrl.ToConnectionString(
            "postgresql://bud_tack_app:s%40cret@db.local:6543/app_dev?sslmode=require");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Equal("db.local", builder.Host);
        Assert.Equal(6543, builder.Port);
        Assert.Equal("bud_tack_app", builder.Username);
        Assert.Equal("s@cret", builder.Password);
        Assert.Equal("app_dev", builder.Database);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void ToConnectionString_withoutPort_usesPostgresDefault()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            PostgresUrl.ToConnectionString("postgresql://user:pw@localhost/app_dev"));

        Assert.Equal(5432, builder.Port);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("mysql://user:pw@localhost:3306/app_dev")]
    [InlineData("postgresql://localhost:5432/app_dev")]
    [InlineData("postgresql://user:pw@localhost:5432/")]
    public void ToConnectionString_withInvalidUrl_isRejected(string url)
    {
        Assert.Throws<FormatException>(() => PostgresUrl.ToConnectionString(url));
    }
}
