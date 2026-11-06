using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Domain;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>transactions.budgets in memory, seen through its owner.</summary>
internal sealed class InMemoryBudgets : IBudgetsRepository
{
    public List<BudgetRow> Rows { get; } = [];

    public Task<(IReadOnlyList<BudgetRow> Rows, long Total)> ListAsync(
        Guid owner, Guid? categoryId, Paging paging, CancellationToken cancellationToken)
    {
        var all = Rows
            .Where(b => b.OwnedBy == owner && (categoryId is null || b.CategoryId == categoryId))
            .OrderByDescending(b => b.PeriodStart)
            .ToList();
        return Task.FromResult<(IReadOnlyList<BudgetRow>, long)>(([.. all.Skip(paging.Offset).Take(paging.Limit)], all.Count));
    }

    public Task<BudgetRow?> FindAsync(Guid owner, Guid budgetId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Find(b => b.Id == budgetId && b.OwnedBy == owner));

    public Task AddAsync(BudgetRow budget, CancellationToken cancellationToken)
    {
        Rows.Add(budget);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(BudgetRow budget, CancellationToken cancellationToken)
    {
        var index = Rows.FindIndex(b => b.Id == budget.Id && b.OwnedBy == budget.OwnedBy);
        if (index < 0)
        {
            return Task.FromResult(false);
        }

        Rows[index] = budget;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid owner, Guid budgetId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.RemoveAll(b => b.Id == budgetId && b.OwnedBy == owner) == 1);
}
