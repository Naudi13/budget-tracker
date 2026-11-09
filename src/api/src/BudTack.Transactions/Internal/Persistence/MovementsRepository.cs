using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Domain;
using Npgsql;
using NpgsqlTypes;

namespace BudTack.Transactions.Internal.Persistence;

/// <summary>
/// transactions.transactions and .transaction_details over the shared
/// <see cref="NpgsqlDataSource"/>, as bud_tack_app. Every statement carries the
/// owner; deleted movements are never read back (BR-04). A line's type is read
/// from its subcategory's category, never stored (BR-01).
/// </summary>
internal sealed class MovementsRepository(NpgsqlDataSource dataSource) : IMovementsRepository
{
    private const string ForeignKeyViolation = "23503";

    public async Task<IReadOnlyDictionary<Guid, SubcategoryUse>> FindSubcategoryUsesAsync(
        IReadOnlyCollection<Guid> subcategoryIds, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT s.id, c.type, s.is_active AND c.is_active
            FROM transactions.subcategories s JOIN transactions.categories c ON c.id = s.category_id
            WHERE s.id = ANY($1)
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = subcategoryIds.ToArray() });

        var uses = new Dictionary<Guid, SubcategoryUse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var use = new SubcategoryUse(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2));
            uses[use.SubcategoryId] = use;
        }

        return uses;
    }

    public async Task AddAsync(Movement movement, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using var batch = new NpgsqlBatch(connection, transaction);
        var header = new NpgsqlBatchCommand(
            """
            INSERT INTO transactions.transactions (id, owned_by, transaction_date, description, created_by, updated_by)
            VALUES ($1, $2, $3, $4, $2, $2)
            """);
        header.Parameters.Add(new NpgsqlParameter { Value = movement.Id });
        header.Parameters.Add(new NpgsqlParameter { Value = movement.OwnedBy });
        header.Parameters.Add(new NpgsqlParameter { Value = movement.TransactionDate });
        header.Parameters.Add(new NpgsqlParameter { Value = movement.Description });
        batch.BatchCommands.Add(header);

        // line_number keeps the order the lines were entered (Transactions005).
        foreach (var (line, index) in movement.Lines.Select((line, index) => (line, index)))
        {
            var detail = new NpgsqlBatchCommand(
                """
                INSERT INTO transactions.transaction_details (id, transaction_id, line_number, subcategory_id, amount, created_by, updated_by)
                VALUES ($1, $2, $6, $3, $4, $5, $5)
                """);
            detail.Parameters.Add(new NpgsqlParameter { Value = line.Id });
            detail.Parameters.Add(new NpgsqlParameter { Value = movement.Id });
            detail.Parameters.Add(new NpgsqlParameter { Value = line.SubcategoryId });
            detail.Parameters.Add(new NpgsqlParameter { Value = line.Amount });
            detail.Parameters.Add(new NpgsqlParameter { Value = movement.OwnedBy });
            detail.Parameters.Add(new NpgsqlParameter { Value = (short)(index + 1) });
            batch.BatchCommands.Add(detail);
        }

        try
        {
            await batch.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState == ForeignKeyViolation)
        {
            // A subcategory was deleted between the check and the insert; nothing was stored.
            throw new TransactionsValidationException([new FieldError("details", "a subcategory no longer exists")]);
        }
    }

    public async Task<(IReadOnlyList<Movement> Rows, long Total)> ListAsync(
        Guid? owner, bool deleted, Paging paging, CancellationToken cancellationToken)
    {
        const string Where = "WHERE ($1::uuid IS NULL OR owned_by = $1) AND (deleted_at IS NOT NULL) = $2";
        var headers = new List<Movement>();
        await using (var command = dataSource.CreateCommand(
            $"""
            SELECT id, owned_by, transaction_date, description, deleted_at, deleted_by, deletion_reason
            FROM transactions.transactions {Where}
            ORDER BY transaction_date DESC, created_at DESC, id DESC LIMIT $3 OFFSET $4
            """))
        {
            command.Parameters.Add(Owner(owner));
            command.Parameters.Add(new NpgsqlParameter { Value = deleted });
            command.Parameters.Add(new NpgsqlParameter { Value = paging.Limit });
            command.Parameters.Add(new NpgsqlParameter { Value = paging.Offset });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                headers.Add(new Movement(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetFieldValue<DateOnly>(2),
                    reader.GetString(3),
                    [],
                    reader.IsDBNull(4)
                        ? null
                        : new MovementDeletion(reader.GetFieldValue<DateTimeOffset>(4), reader.GetGuid(5), reader.GetString(6))));
            }
        }

        var lines = await LinesAsync([.. headers.Select(h => h.Id)], cancellationToken).ConfigureAwait(false);

        await using var count = dataSource.CreateCommand($"SELECT count(*) FROM transactions.transactions {Where}");
        count.Parameters.Add(Owner(owner));
        count.Parameters.Add(new NpgsqlParameter { Value = deleted });
        var total = (long)(await count.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;

        var rows = headers
            .Select(h => h with { Lines = lines.TryGetValue(h.Id, out var l) ? l : [] })
            .ToList();
        return (rows, total);
    }

    public async Task<(decimal Income, decimal Expenses)> TotalsAsync(Guid? owner, CancellationToken cancellationToken)
    {
        // One pass over the undeleted lines; each line's type is its category's (BR-01).
        await using var command = dataSource.CreateCommand(
            """
            SELECT coalesce(sum(d.amount) FILTER (WHERE c.type = 'income'), 0),
                   coalesce(sum(d.amount) FILTER (WHERE c.type = 'expense'), 0)
            FROM transactions.transactions t
            JOIN transactions.transaction_details d ON d.transaction_id = t.id
            JOIN transactions.subcategories s ON s.id = d.subcategory_id
            JOIN transactions.categories c ON c.id = s.category_id
            WHERE ($1::uuid IS NULL OR t.owned_by = $1) AND t.deleted_at IS NULL
            """);
        command.Parameters.Add(Owner(owner));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return (reader.GetDecimal(0), reader.GetDecimal(1));
    }

    public async Task<bool> SoftDeleteAsync(
        Guid actor, Guid? owner, Guid movementId, string reason, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            UPDATE transactions.transactions
            SET deleted_at = now(), deleted_by = $2, deletion_reason = $4, updated_at = now(), updated_by = $2
            WHERE id = $1 AND ($3::uuid IS NULL OR owned_by = $3) AND deleted_at IS NULL
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = movementId });
        command.Parameters.Add(new NpgsqlParameter { Value = actor });
        command.Parameters.Add(Owner(owner));
        command.Parameters.Add(new NpgsqlParameter { Value = reason });
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    private static NpgsqlParameter Owner(Guid? owner) =>
        new() { Value = (object?)owner ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Uuid };

    private async Task<Dictionary<Guid, List<MovementLine>>> LinesAsync(Guid[] movementIds, CancellationToken cancellationToken)
    {
        var lines = new Dictionary<Guid, List<MovementLine>>();
        if (movementIds.Length == 0)
        {
            return lines;
        }

        await using var command = dataSource.CreateCommand(
            """
            SELECT d.transaction_id, d.id, d.subcategory_id, d.amount, c.type
            FROM transactions.transaction_details d
            JOIN transactions.subcategories s ON s.id = d.subcategory_id
            JOIN transactions.categories c ON c.id = s.category_id
            WHERE d.transaction_id = ANY($1)
            ORDER BY d.transaction_id, d.line_number
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = movementIds });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var movementId = reader.GetGuid(0);
            if (!lines.TryGetValue(movementId, out var list))
            {
                lines[movementId] = list = [];
            }

            list.Add(new MovementLine(reader.GetGuid(1), reader.GetGuid(2), reader.GetDecimal(3), reader.GetString(4)));
        }

        return lines;
    }
}
