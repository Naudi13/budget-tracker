using BudTack.Identity.Internal.Application;
using BudTack.Identity.Internal.Domain;
using Npgsql;

namespace BudTack.Identity.Internal.Persistence;

/// <summary>
/// identity.account_tokens over the shared <see cref="NpgsqlDataSource"/>. Each
/// operation is one statement, so two uses of a link at once cannot both win.
/// </summary>
internal sealed class AccountTokensRepository(NpgsqlDataSource dataSource) : IAccountTokensRepository
{
    public async Task ReplaceAsync(AccountToken token, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            WITH cancelled AS (
                UPDATE identity.account_tokens SET used_at = now() WHERE user_id = $2 AND used_at IS NULL
            )
            INSERT INTO identity.account_tokens (id, user_id, purpose, token_hash, expires_at, created_by)
            VALUES ($1, $2, $3, $4, $5, $6)
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = token.Id });
        command.Parameters.Add(new NpgsqlParameter { Value = token.UserId });
        command.Parameters.Add(new NpgsqlParameter { Value = token.Purpose });
        command.Parameters.Add(new NpgsqlParameter { Value = token.TokenHash });
        command.Parameters.Add(new NpgsqlParameter { Value = token.ExpiresAt });
        command.Parameters.Add(new NpgsqlParameter { Value = token.CreatedBy });
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ConfirmAsync(
        string tokenHash, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            WITH spent AS (
                UPDATE identity.account_tokens SET used_at = $3
                WHERE token_hash = $1 AND used_at IS NULL AND expires_at > $3
                RETURNING user_id
            )
            UPDATE identity.users u SET password_hash = $2, updated_at = $3, updated_by = u.id
            FROM spent WHERE u.id = spent.user_id
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = tokenHash });
        command.Parameters.Add(new NpgsqlParameter { Value = passwordHash });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }
}
