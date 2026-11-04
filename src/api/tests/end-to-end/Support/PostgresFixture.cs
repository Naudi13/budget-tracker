using Testcontainers.PostgreSql;
using Xunit;

namespace BudTack.EndToEnd.Tests.Support;

/// <summary>
/// One throwaway PostgreSQL 17 for the whole end-to-end run, the same major
/// version as bud-tack-infra. Needs Docker.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("app_e2e")
        .WithUsername("e2e")
        .WithPassword("e2e")
        .Build();

    /// <summary>The container's address as a <c>DATABASE_URL</c>, the shape the API reads.</summary>
    public string DatabaseUrl =>
        $"postgresql://e2e:e2e@{_container.Hostname}:{_container.GetMappedPublicPort(5432)}/app_e2e";

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgresTestGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
