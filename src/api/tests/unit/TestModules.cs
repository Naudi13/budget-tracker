using BudTack.Identity;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BudTack.Unit.Tests;

/// <summary>
/// What the host gives every module: logging, a data source (never opened by a
/// unit test) and identity's settings.
/// </summary>
internal static class TestModules
{
    public static readonly IdentitySettings Identity =
        new("unit-test-secret-unit-test-secret-0123", TimeSpan.FromHours(1));

    public static IServiceCollection HostServices() =>
        new ServiceCollection()
            .AddLogging()
            .AddSingleton(_ => NpgsqlDataSource.Create("Host=localhost;Database=never_opened"));
}
