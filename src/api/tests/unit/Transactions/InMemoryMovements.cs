using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Domain;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>transactions.transactions and .transaction_details in memory, seen through their owner.</summary>
internal sealed class InMemoryMovements : IMovementsRepository
{
    public Dictionary<Guid, (Guid Owner, SubcategoryUse Use)> Subcategories { get; } = [];

    public List<Movement> Movements { get; } = [];

    public HashSet<Guid> Deleted { get; } = [];

    public Task<IReadOnlyDictionary<Guid, SubcategoryUse>> FindSubcategoryUsesAsync(
        IReadOnlyCollection<Guid> subcategoryIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, SubcategoryUse>>(
            subcategoryIds
                .Where(id => Subcategories.ContainsKey(id))
                .ToDictionary(id => id, id => Subcategories[id].Use));

    public Task AddAsync(Movement movement, CancellationToken cancellationToken)
    {
        Movements.Add(movement);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<Movement> Rows, long Total)> ListAsync(Guid owner, Paging paging, CancellationToken cancellationToken)
    {
        var all = Movements
            .Where(m => m.OwnedBy == owner && !Deleted.Contains(m.Id))
            .OrderByDescending(m => m.TransactionDate)
            .ThenByDescending(m => Movements.IndexOf(m))
            .ToList();
        return Task.FromResult<(IReadOnlyList<Movement>, long)>(([.. all.Skip(paging.Offset).Take(paging.Limit)], all.Count));
    }

    public Task<(decimal Income, decimal Expenses)> TotalsAsync(Guid owner, CancellationToken cancellationToken)
    {
        var lines = Movements.Where(m => m.OwnedBy == owner && !Deleted.Contains(m.Id)).SelectMany(m => m.Lines).ToList();
        return Task.FromResult((
            lines.Where(l => l.Type == "income").Sum(l => l.Amount),
            lines.Where(l => l.Type == "expense").Sum(l => l.Amount)));
    }

    public Task<bool> SoftDeleteAsync(Guid owner, Guid movementId, CancellationToken cancellationToken) =>
        Task.FromResult(Movements.Exists(m => m.Id == movementId && m.OwnedBy == owner) && Deleted.Add(movementId));
}
