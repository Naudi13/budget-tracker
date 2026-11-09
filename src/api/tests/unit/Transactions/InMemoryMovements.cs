using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Domain;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>transactions.transactions and .transaction_details in memory, seen through their owner.</summary>
internal sealed class InMemoryMovements : IMovementsRepository
{
    public Dictionary<Guid, (Guid Owner, SubcategoryUse Use)> Subcategories { get; } = [];

    public List<Movement> Movements { get; } = [];

    public HashSet<Guid> Deleted { get; } = [];

    public Dictionary<Guid, MovementDeletion> Deletions { get; } = [];

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

    public Task<(IReadOnlyList<Movement> Rows, long Total)> ListAsync(
        Guid? owner, bool deleted, Paging paging, CancellationToken cancellationToken)
    {
        var all = Movements
            .Where(m => (owner is null || m.OwnedBy == owner) && Deleted.Contains(m.Id) == deleted)
            .Select(m => m with { Deletion = Deletions.GetValueOrDefault(m.Id) })
            .OrderByDescending(m => m.TransactionDate)
            .ThenByDescending(m => Movements.FindIndex(x => x.Id == m.Id))
            .ToList();
        return Task.FromResult<(IReadOnlyList<Movement>, long)>(([.. all.Skip(paging.Offset).Take(paging.Limit)], all.Count));
    }

    public Task<(decimal Income, decimal Expenses)> TotalsAsync(Guid? owner, CancellationToken cancellationToken)
    {
        var lines = Movements.Where(m => (owner is null || m.OwnedBy == owner) && !Deleted.Contains(m.Id)).SelectMany(m => m.Lines).ToList();
        return Task.FromResult((
            lines.Where(l => l.Type == "income").Sum(l => l.Amount),
            lines.Where(l => l.Type == "expense").Sum(l => l.Amount)));
    }

    public Task<bool> SoftDeleteAsync(Guid actor, Guid? owner, Guid movementId, string reason, CancellationToken cancellationToken)
    {
        if (!Movements.Exists(m => m.Id == movementId && (owner is null || m.OwnedBy == owner)) || !Deleted.Add(movementId))
        {
            return Task.FromResult(false);
        }

        Deletions[movementId] = new MovementDeletion(DateTimeOffset.UnixEpoch, actor, reason);
        return Task.FromResult(true);
    }
}
