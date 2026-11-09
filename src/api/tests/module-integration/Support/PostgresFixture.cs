using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace BudTack.ModuleIntegration.Tests.Support;

/// <summary>
/// One throwaway PostgreSQL 17 per run with the migrated schema of bud-tack-db
/// (tests/schema/budtack.sql). Each test empties the tables it uses. Needs Docker.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("app_it")
        .WithUsername("it")
        .WithPassword("it")
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        var schema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
        var result = await _container.ExecScriptAsync(schema);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException("The test schema did not apply: " + result.Stderr);
        }

        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());
    }

    public async Task ResetAsync()
    {
        await using var command = DataSource.CreateCommand("TRUNCATE identity.account_tokens, identity.users, transactions.budgets, transactions.transaction_details, transactions.transactions, transactions.subcategories, transactions.categories");
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (DataSource is not null)
        {
            await DataSource.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
