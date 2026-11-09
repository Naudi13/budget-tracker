using BudTack.Transactions.Internal.Domain;

namespace BudTack.Transactions.Internal.Application;

/// <summary>transactions.transactions and .transaction_details. A null <c>owner</c> means every account's (ADMIN).</summary>
internal interface IMovementsRepository
{
    /// <summary>The subcategories among <paramref name="subcategoryIds"/> that exist.</summary>
    Task<IReadOnlyDictionary<Guid, SubcategoryUse>> FindSubcategoryUsesAsync(
        IReadOnlyCollection<Guid> subcategoryIds, CancellationToken cancellationToken);

    /// <summary>Stores a movement and its lines in one database transaction: all of it or none.</summary>
    Task AddAsync(Movement movement, CancellationToken cancellationToken);

    /// <summary>Movements that are not deleted (BR-04), or only the deleted ones, newest first, with their lines.</summary>
    Task<(IReadOnlyList<Movement> Rows, long Total)> ListAsync(
        Guid? owner, bool deleted, Paging paging, CancellationToken cancellationToken);

    /// <summary>Income and expense sums over undeleted movements (BR-03, BR-04).</summary>
    Task<(decimal Income, decimal Expenses)> TotalsAsync(Guid? owner, CancellationToken cancellationToken);

    /// <summary>Sets deleted_at, deleted_by and the reason (BR-04); false when there is no such undeleted movement.</summary>
    Task<bool> SoftDeleteAsync(Guid actor, Guid? owner, Guid movementId, string reason, CancellationToken cancellationToken);
}
