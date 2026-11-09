using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Application;
using Npgsql;
using NpgsqlTypes;

namespace BudTack.Identity.Internal.Persistence;

/// <summary>identity.users over the shared <see cref="NpgsqlDataSource"/>, as bud_tack_app.</summary>
internal sealed class UsersRepository(NpgsqlDataSource dataSource) : IUsersRepository
{
    private const string UniqueViolation = "23505";
    private const string EmailIndex = "idx_users_email";

    public async Task AddAsync(Domain.User user, Guid actor, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO identity.users (id, name, email, password_hash, role, created_by, updated_by)
            VALUES ($1, $2, $3, $4, $5, $6, $6)
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = user.Id });
        command.Parameters.Add(new NpgsqlParameter { Value = user.Name });
        command.Parameters.Add(new NpgsqlParameter { Value = user.Email });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)user.PasswordHash ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Varchar });
        command.Parameters.Add(new NpgsqlParameter { Value = user.Role });
        command.Parameters.Add(new NpgsqlParameter { Value = actor });

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception)
            when (exception.SqlState == UniqueViolation && exception.ConstraintName == EmailIndex)
        {
            throw new EmailAlreadyInUseException();
        }
    }

    public async Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var names = new Dictionary<Guid, string>();
        await using var command = dataSource.CreateCommand("SELECT id, name FROM identity.users WHERE id = ANY($1)");
        command.Parameters.Add(new NpgsqlParameter { Value = ids.ToArray() });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            names[reader.GetGuid(0)] = reader.GetString(1);
        }

        return names;
    }

    public async Task<bool> IsActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("SELECT is_active FROM identity.users WHERE id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true;
    }

    public async Task<Domain.User?> SetActiveAsync(Guid actor, Guid id, bool isActive, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            WITH cancelled AS (
                UPDATE identity.account_tokens SET used_at = now() WHERE user_id = $1 AND used_at IS NULL AND NOT $2
            )
            UPDATE identity.users SET is_active = $2, updated_at = now(), updated_by = $3
            WHERE id = $1
            RETURNING id, name, email, password_hash, role, is_active
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        command.Parameters.Add(new NpgsqlParameter { Value = isActive });
        command.Parameters.Add(new NpgsqlParameter { Value = actor });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadUser(reader) : null;
    }

    public async Task<bool> DeleteNeverActivatedAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // The links go first: their key to the account is ON DELETE RESTRICT.
        await using (var links = new NpgsqlCommand(
            """
            DELETE FROM identity.account_tokens t USING identity.users u
            WHERE t.user_id = u.id AND u.id = $1 AND u.password_hash IS NULL
            """, connection, transaction))
        {
            links.Parameters.Add(new NpgsqlParameter { Value = id });
            await links.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var account = new NpgsqlCommand(
            "DELETE FROM identity.users WHERE id = $1 AND password_hash IS NULL", connection, transaction);
        account.Parameters.Add(new NpgsqlParameter { Value = id });
        var deleted = await account.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return deleted;
    }

    public async Task<Domain.User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT id, name, email, password_hash, role, is_active FROM identity.users WHERE id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = id });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadUser(reader) : null;
    }

    public async Task<Domain.User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT id, name, email, password_hash, role, is_active FROM identity.users WHERE email = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = email });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return ReadUser(reader);
    }

    public async Task<(IReadOnlyList<Domain.User> Rows, long Total)> ListAsync(
        int limit, int offset, CancellationToken cancellationToken)
    {
        var rows = new List<Domain.User>();
        await using (var command = dataSource.CreateCommand(
            "SELECT id, name, email, password_hash, role, is_active FROM identity.users ORDER BY email LIMIT $1 OFFSET $2"))
        {
            command.Parameters.Add(new NpgsqlParameter { Value = limit });
            command.Parameters.Add(new NpgsqlParameter { Value = offset });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(ReadUser(reader));
            }
        }

        await using var count = dataSource.CreateCommand("SELECT count(*) FROM identity.users");
        var total = (long)(await count.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
        return (rows, total);
    }

    private static Domain.User ReadUser(NpgsqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4), reader.GetBoolean(5));
}
