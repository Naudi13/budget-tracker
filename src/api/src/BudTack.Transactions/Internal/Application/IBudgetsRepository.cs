using BudTack.Transactions.Internal.Domain;

namespace BudTack.Transactions.Internal.Application;

/// <summary>transactions.budgets, shared by the whole business (ADR-009).</summary>
internal interface IBudgetsRepository
{
    /// <summary>The budgets, newest period first, optionally for one category.</summary>
    Task<(IReadOnlyList<BudgetRow> Rows, long Total)> ListAsync(
        Guid? categoryId, Paging paging, CancellationToken cancellationToken);

    /// <summary>
    /// The budgets in force on <paramref name="today"/> and what was spent inside their
    /// periods, from every account (BR-08): one category's, or every expense category's.
    /// </summary>
    Task<(int Budgets, decimal Amount, decimal Spent)> ProgressAsync(
        Guid? categoryId, DateOnly today, CancellationToken cancellationToken);

    Task<BudgetRow?> FindAsync(Guid budgetId, CancellationToken cancellationToken);

    Task AddAsync(BudgetRow budget, Guid actor, CancellationToken cancellationToken);

    /// <summary>Writes every field of <paramref name="budget"/>; false when there is no such budget.</summary>
    Task<bool> UpdateAsync(BudgetRow budget, Guid actor, CancellationToken cancellationToken);

    /// <summary>Removes the row (BR-06); false when there is no such budget.</summary>
    Task<bool> DeleteAsync(Guid budgetId, CancellationToken cancellationToken);
}
