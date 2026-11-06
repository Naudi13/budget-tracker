using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Domain;
using Npgsql;
using NpgsqlTypes;
using DomainCategory = BudTack.Transactions.Internal.Domain.Category;
using DomainSubcategory = BudTack.Transactions.Internal.Domain.Subcategory;

namespace BudTack.Transactions.Internal.Persistence;

/// <summary>
/// transactions.categories and .subcategories over the shared
/// <see cref="NpgsqlDataSource"/>, as bud_tack_app. Every statement carries the
/// owner, so another owner's row is never read, changed or deleted. In V1 the
/// owner is also the actor of every write (created_by, updated_by).
/// </summary>
internal sealed class CategoriesRepository(NpgsqlDataSource dataSource) : ICategoriesRepository
{
    private const string UniqueViolation = "23505";
    private const string ForeignKeyViolation = "23503";

    public async Task<(IReadOnlyList<DomainCategory> Rows, long Total)> ListCategoriesAsync(
        Guid owner, string? type, Paging paging, CancellationToken cancellationToken)
    {
        const string Where = "WHERE owned_by = $1 AND ($2::text IS NULL OR type = $2)";
        var rows = new List<DomainCategory>();
        await using (var command = dataSource.CreateCommand(
            $"""
            SELECT id, owned_by, name, type, is_active FROM transactions.categories {Where}
            ORDER BY type, lower(name), id LIMIT $3 OFFSET $4
            """))
        {
            command.Parameters.Add(new NpgsqlParameter { Value = owner });
            command.Parameters.Add(Text(type));
            command.Parameters.Add(new NpgsqlParameter { Value = paging.Limit });
            command.Parameters.Add(new NpgsqlParameter { Value = paging.Offset });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(ReadCategory(reader));
            }
        }

        await using var count = dataSource.CreateCommand($"SELECT count(*) FROM transactions.categories {Where}");
        count.Parameters.Add(new NpgsqlParameter { Value = owner });
        count.Parameters.Add(Text(type));
        var total = (long)(await count.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
        return (rows, total);
    }

    public async Task<DomainCategory?> FindCategoryAsync(Guid owner, Guid categoryId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT id, owned_by, name, type, is_active FROM transactions.categories WHERE id = $1 AND owned_by = $2");
        command.Parameters.Add(new NpgsqlParameter { Value = categoryId });
        command.Parameters.Add(new NpgsqlParameter { Value = owner });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadCategory(reader) : null;
    }

    public async Task AddCategoryAsync(DomainCategory category, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO transactions.categories (id, owned_by, name, type, is_active, created_by, updated_by)
            VALUES ($1, $2, $3, $4, $5, $2, $2)
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = category.Id });
        command.Parameters.Add(new NpgsqlParameter { Value = category.OwnedBy });
        command.Parameters.Add(new NpgsqlParameter { Value = category.Name });
        command.Parameters.Add(new NpgsqlParameter { Value = category.Type });
        command.Parameters.Add(new NpgsqlParameter { Value = category.IsActive });
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState == UniqueViolation)
        {
            throw new TransactionsConflictException($"An {category.Type} category with this name already exists.");
        }
    }

    public async Task<bool> DeleteCategoryAsync(Guid owner, Guid categoryId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "DELETE FROM transactions.categories WHERE id = $1 AND owned_by = $2");
        command.Parameters.Add(new NpgsqlParameter { Value = categoryId });
        command.Parameters.Add(new NpgsqlParameter { Value = owner });
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }
        catch (PostgresException exception) when (exception.SqlState == ForeignKeyViolation)
        {
            // ON DELETE RESTRICT from subcategories (and, later, budgets): BR-05.
            throw new TransactionsConflictException(
                "The category still has subcategories or budgets; deactivate it instead.");
        }
    }

    public async Task<DomainCategory?> SetCategoryActiveAsync(
        Guid owner, Guid categoryId, bool isActive, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            UPDATE transactions.categories SET is_active = $3, updated_at = now(), updated_by = $2
            WHERE id = $1 AND owned_by = $2
            RETURNING id, owned_by, name, type, is_active
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = categoryId });
        command.Parameters.Add(new NpgsqlParameter { Value = owner });
        command.Parameters.Add(new NpgsqlParameter { Value = isActive });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadCategory(reader) : null;
    }

    public async Task<(IReadOnlyList<DomainSubcategory> Rows, long Total)> ListSubcategoriesAsync(
        Guid categoryId, Paging paging, CancellationToken cancellationToken)
    {
        var rows = new List<DomainSubcategory>();
        await using (var command = dataSource.CreateCommand(
            """
            SELECT id, category_id, name, is_active FROM transactions.subcategories WHERE category_id = $1
            ORDER BY lower(name), id LIMIT $2 OFFSET $3
            """))
        {
            command.Parameters.Add(new NpgsqlParameter { Value = categoryId });
            command.Parameters.Add(new NpgsqlParameter { Value = paging.Limit });
            command.Parameters.Add(new NpgsqlParameter { Value = paging.Offset });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(ReadSubcategory(reader));
            }
        }

        await using var count = dataSource.CreateCommand(
            "SELECT count(*) FROM transactions.subcategories WHERE category_id = $1");
        count.Parameters.Add(new NpgsqlParameter { Value = categoryId });
        var total = (long)(await count.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
        return (rows, total);
    }

    public async Task AddSubcategoryAsync(DomainSubcategory subcategory, Guid owner, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO transactions.subcategories (id, category_id, name, is_active, created_by, updated_by)
            VALUES ($1, $2, $3, $4, $5, $5)
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = subcategory.Id });
        command.Parameters.Add(new NpgsqlParameter { Value = subcategory.CategoryId });
        command.Parameters.Add(new NpgsqlParameter { Value = subcategory.Name });
        command.Parameters.Add(new NpgsqlParameter { Value = subcategory.IsActive });
        command.Parameters.Add(new NpgsqlParameter { Value = owner });
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException exception) when (exception.SqlState == UniqueViolation)
        {
            throw new TransactionsConflictException("The category already has a subcategory with this name.");
        }
        catch (PostgresException exception) when (exception.SqlState == ForeignKeyViolation)
        {
            // The category was deleted between the check and the insert.
            throw new TransactionsNotFoundException(ManageCategories.CategoryNotFound);
        }
    }

    public async Task<bool> DeleteSubcategoryAsync(
        Guid owner, Guid categoryId, Guid subcategoryId, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            DELETE FROM transactions.subcategories s USING transactions.categories c
            WHERE s.id = $1 AND s.category_id = $2 AND c.id = s.category_id AND c.owned_by = $3
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = subcategoryId });
        command.Parameters.Add(new NpgsqlParameter { Value = categoryId });
        command.Parameters.Add(new NpgsqlParameter { Value = owner });
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
        }
        catch (PostgresException exception) when (exception.SqlState == ForeignKeyViolation)
        {
            // ON DELETE RESTRICT from transaction details, once HU-04 adds them: BR-05.
            throw new TransactionsConflictException(
                "The subcategory is used by transactions; deactivate it instead.");
        }
    }

    public async Task<DomainSubcategory?> SetSubcategoryActiveAsync(
        Guid owner, Guid categoryId, Guid subcategoryId, bool isActive, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            UPDATE transactions.subcategories s SET is_active = $4, updated_at = now(), updated_by = $3
            FROM transactions.categories c
            WHERE s.id = $1 AND s.category_id = $2 AND c.id = s.category_id AND c.owned_by = $3
            RETURNING s.id, s.category_id, s.name, s.is_active
            """);
        command.Parameters.Add(new NpgsqlParameter { Value = subcategoryId });
        command.Parameters.Add(new NpgsqlParameter { Value = categoryId });
        command.Parameters.Add(new NpgsqlParameter { Value = owner });
        command.Parameters.Add(new NpgsqlParameter { Value = isActive });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadSubcategory(reader) : null;
    }

    private static NpgsqlParameter Text(string? value) =>
        new() { Value = (object?)value ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Text };

    private static DomainCategory ReadCategory(NpgsqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetBoolean(4));

    private static DomainSubcategory ReadSubcategory(NpgsqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetBoolean(3));
}
