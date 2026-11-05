using Testcontainers.PostgreSql;
using Xunit;

namespace BudTack.EndToEnd.Tests.Support;

/// <summary>
/// One throwaway PostgreSQL 17 for the whole end-to-end run, the same major
/// version as bud-tack-infra, with the migrated schema of bud-tack-db
/// (tests/schema/budtack.sql). Needs Docker.
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

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        var schema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
        var result = await _container.ExecScriptAsync(schema);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("The test schema did not apply: " + result.Stderr);
        }
    }

    /// <summary>The first column of the first row, read as the container's superuser, for assertions on stored rows.</summary>
    public async Task<object?> ScalarAsync(string sql, params object[] parameters)
    {
        await using var dataSource = Npgsql.NpgsqlDataSource.Create(_container.GetConnectionString());
        await using var command = dataSource.CreateCommand(sql);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(new Npgsql.NpgsqlParameter { Value = parameter });
        }

        return await command.ExecuteScalarAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgresTestGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
