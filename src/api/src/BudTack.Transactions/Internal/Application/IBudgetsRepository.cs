using BudTack.Transactions.Internal.Domain;

namespace BudTack.Transactions.Internal.Application;

/// <summary>transactions.budgets, always seen through one owner.</summary>
internal interface IBudgetsRepository
{
    /// <summary>The owner's budgets, newest period first, optionally for one category.</summary>
    Task<(IReadOnlyList<BudgetRow> Rows, long Total)> ListAsync(
        Guid owner, Guid? categoryId, Paging paging, CancellationToken cancellationToken);

    Task<BudgetRow?> FindAsync(Guid owner, Guid budgetId, CancellationToken cancellationToken);

    Task AddAsync(BudgetRow budget, CancellationToken cancellationToken);

    /// <summary>Writes every field of <paramref name="budget"/>; false when the owner has no such budget.</summary>
    Task<bool> UpdateAsync(BudgetRow budget, CancellationToken cancellationToken);

    /// <summary>Removes the row (BR-06); false when the owner has no such budget.</summary>
    Task<bool> DeleteAsync(Guid owner, Guid budgetId, CancellationToken cancellationToken);
}
