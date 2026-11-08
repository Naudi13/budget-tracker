using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Application;
using Npgsql;

namespace BudTack.Identity.Internal.Persistence;

/// <summary>identity.users over the shared <see cref="NpgsqlDataSource"/>, as bud_tack_app.</summary>
internal sealed class UsersRepository(NpgsqlDataSource dataSource) : IUsersRepository
{
    private const string UniqueViolation = "23505";
    private const string EmailIndex = "idx_users_email";

    public async Task AddAsync(Domain.User user, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO identity.users (id, name, email, password_hash, role, created_by, updated_by)
            VALUES ($1, $2, $3, $4, $5, $1, $1)
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = user.Id });
        command.Parameters.Add(new NpgsqlParameter { Value = user.Name });
        command.Parameters.Add(new NpgsqlParameter { Value = user.Email });
        command.Parameters.Add(new NpgsqlParameter { Value = user.PasswordHash });
        command.Parameters.Add(new NpgsqlParameter { Value = user.Role });

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

    public async Task<Domain.User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT id, name, email, password_hash, role FROM identity.users WHERE email = $1");
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
            "SELECT id, name, email, password_hash, role FROM identity.users ORDER BY email LIMIT $1 OFFSET $2"))
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
        new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4));
}
