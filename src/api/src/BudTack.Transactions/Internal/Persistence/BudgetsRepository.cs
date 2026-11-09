using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Domain;
using Npgsql;
using NpgsqlTypes;

namespace BudTack.Transactions.Internal.Persistence;

/// <summary>
/// transactions.budgets over the shared <see cref="NpgsqlDataSource"/>, as
/// bud_tack_app. The database's own checks
/// (amount, period) stand behind the rules; a category deleted in between
/// surfaces as its foreign key.
/// </summary>
internal sealed class BudgetsRepository(NpgsqlDataSource dataSource) : IBudgetsRepository
{
    private const string ForeignKeyViolation = "23503";
    private const string Columns = "id, category_id, amount, period_start, period_end";

    public async Task<(IReadOnlyList<BudgetRow> Rows, long Total)> ListAsync(
        Guid? categoryId, Paging paging, CancellationToken cancellationToken)
    {
        const string Where = "WHERE ($1::uuid IS NULL OR category_id = $1)";
        var rows = new List<BudgetRow>();
        await using (var command = dataSource.CreateCommand(
            $"""
            SELECT {Columns} FROM transactions.budgets {Where}
            ORDER BY period_start DESC, created_at DESC, id DESC LIMIT $2 OFFSET $3
            """))
        {
            command.Parameters.Add(CategoryFilter(categoryId));
            command.Parameters.Add(new NpgsqlParameter { Value = paging.Limit });
            command.Parameters.Add(new NpgsqlParameter { Value = paging.Offset });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(Read(reader));
            }
        }

        await using var count = dataSource.CreateCommand($"SELECT count(*) FROM transactions.budgets {Where}");
        count.Parameters.Add(CategoryFilter(categoryId));
        var total = (long)(await count.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
        return (rows, total);
    }

    public async Task<BudgetRow?> FindAsync(Guid budgetId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            $"SELECT {Columns} FROM transactions.budgets WHERE id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = budgetId });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Read(reader) : null;
    }

    public async Task AddAsync(BudgetRow budget, Guid actor, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO transactions.budgets (id, category_id, amount, period_start, period_end, created_by, updated_by)
            VALUES ($1, $3, $4, $5, $6, $2, $2)
            """);
        AddFields(command, budget, actor);
        await ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(BudgetRow budget, Guid actor, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            UPDATE transactions.budgets
            SET category_id = $3, amount = $4, period_start = $5, period_end = $6, updated_at = now(), updated_by = $2
            WHERE id = $1
            """);
        AddFields(command, budget, actor);
        return await ExecuteAsync(command, cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<bool> DeleteAsync(Guid budgetId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("DELETE FROM transactions.budgets WHERE id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = budgetId });
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    private static async Task<int> ExecuteAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState == ForeignKeyViolation)
        {
            // The category was deleted between the check and the write.
            throw new TransactionsValidationException([new FieldError("categoryId", "does not exist")]);
        }
    }

    private static void AddFields(NpgsqlCommand command, BudgetRow budget, Guid actor)
    {
        command.Parameters.Add(new NpgsqlParameter { Value = budget.Id });
        command.Parameters.Add(new NpgsqlParameter { Value = actor });
        command.Parameters.Add(new NpgsqlParameter { Value = budget.CategoryId });
        command.Parameters.Add(new NpgsqlParameter { Value = budget.Amount });
        command.Parameters.Add(new NpgsqlParameter { Value = budget.PeriodStart });
        command.Parameters.Add(new NpgsqlParameter { Value = budget.PeriodEnd });
    }

    private static NpgsqlParameter CategoryFilter(Guid? categoryId) =>
        new() { Value = (object?)categoryId ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Uuid };

    private static BudgetRow Read(NpgsqlDataReader reader) =>
        new(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetDecimal(2),
            reader.GetFieldValue<DateOnly>(3),
            reader.GetFieldValue<DateOnly>(4));
}
