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
            INSERT INTO identity.users (id, name, email, password_hash, created_by, updated_by)
            VALUES ($1, $2, $3, $4, $1, $1)
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = user.Id });
        command.Parameters.Add(new NpgsqlParameter { Value = user.Name });
        command.Parameters.Add(new NpgsqlParameter { Value = user.Email });
        command.Parameters.Add(new NpgsqlParameter { Value = user.PasswordHash });

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
            "SELECT id, name, email, password_hash FROM identity.users WHERE email = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = email });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new Domain.User(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
    }
}
