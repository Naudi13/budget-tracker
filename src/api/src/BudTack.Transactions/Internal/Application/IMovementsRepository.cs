using BudTack.Transactions.Internal.Domain;

namespace BudTack.Transactions.Internal.Application;

/// <summary>transactions.transactions and .transaction_details, always seen through one owner.</summary>
internal interface IMovementsRepository
{
    /// <summary>The owner's subcategories among <paramref name="subcategoryIds"/>; others are left out.</summary>
    Task<IReadOnlyDictionary<Guid, SubcategoryUse>> FindSubcategoryUsesAsync(
        Guid owner, IReadOnlyCollection<Guid> subcategoryIds, CancellationToken cancellationToken);

    /// <summary>Stores a movement and its lines in one database transaction: all of it or none.</summary>
    Task AddAsync(Movement movement, CancellationToken cancellationToken);

    /// <summary>The owner's movements that are not deleted (BR-04), newest first, with their lines.</summary>
    Task<(IReadOnlyList<Movement> Rows, long Total)> ListAsync(Guid owner, Paging paging, CancellationToken cancellationToken);

    /// <summary>Sets deleted_at and deleted_by (BR-04); false when there is no such undeleted movement.</summary>
    Task<bool> SoftDeleteAsync(Guid owner, Guid movementId, CancellationToken cancellationToken);
}
