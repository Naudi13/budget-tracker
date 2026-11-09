using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Domain;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>transactions.budgets in memory.</summary>
internal sealed class InMemoryBudgets : IBudgetsRepository
{
    public List<BudgetRow> Rows { get; } = [];

    public Task<(IReadOnlyList<BudgetRow> Rows, long Total)> ListAsync(
        Guid? categoryId, Paging paging, CancellationToken cancellationToken)
    {
        var all = Rows
            .Where(b => categoryId is null || b.CategoryId == categoryId)
            .OrderByDescending(b => b.PeriodStart)
            .ToList();
        return Task.FromResult<(IReadOnlyList<BudgetRow>, long)>(([.. all.Skip(paging.Offset).Take(paging.Limit)], all.Count));
    }

    public (int Budgets, decimal Amount, decimal Spent) Progress { get; set; }

    public Task<(int Budgets, decimal Amount, decimal Spent)> ProgressAsync(
        Guid? categoryId, DateOnly today, CancellationToken cancellationToken) =>
        Task.FromResult(Progress);

    public Task<BudgetRow?> FindAsync(Guid budgetId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Find(b => b.Id == budgetId));

    public Task AddAsync(BudgetRow budget, Guid actor, CancellationToken cancellationToken)
    {
        Rows.Add(budget);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(BudgetRow budget, Guid actor, CancellationToken cancellationToken)
    {
        var index = Rows.FindIndex(b => b.Id == budget.Id);
        if (index < 0)
        {
            return Task.FromResult(false);
        }

        Rows[index] = budget;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid budgetId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.RemoveAll(b => b.Id == budgetId) == 1);
}
