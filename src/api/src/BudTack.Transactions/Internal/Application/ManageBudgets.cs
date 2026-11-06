using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Domain;
using Microsoft.Extensions.Logging;

namespace BudTack.Transactions.Internal.Application;

/// <summary>
/// HU-07: list, set, change and delete budgets, always for one owner. A new
/// budget and a changed one are held to the same invariants (BR-06), and only
/// an active category of the owner can be budgeted (BR-05). The database checks
/// the amount and the period again, so no path can store a broken row.
/// </summary>
internal sealed partial class ManageBudgets(
    IBudgetsRepository budgets, ICategoriesRepository categories, ILogger<ManageBudgets> logger)
{
    public const string NotFound = "The budget does not exist.";

    public async Task<Page<Budget>> ListAsync(
        Guid owner, int? page, int? limit, Guid? categoryId, CancellationToken cancellationToken)
    {
        var paging = Paging.From(page, limit);
        var (rows, total) = await budgets.ListAsync(owner, categoryId, paging, cancellationToken).ConfigureAwait(false);
        return new Page<Budget>([.. rows.Select(ToContract)], new PageMeta(paging.Page, paging.Limit, total));
    }

    public async Task<Budget> CreateAsync(Guid owner, NewBudget request, CancellationToken cancellationToken)
    {
        var (categoryId, amount, start, end) = BudgetRules.Validate(request);
        BudgetRules.RequireUsable(await categories.FindCategoryAsync(owner, categoryId, cancellationToken).ConfigureAwait(false));

        var budget = new BudgetRow(Guid.CreateVersion7(), owner, categoryId, amount, start, end);
        await budgets.AddAsync(budget, cancellationToken).ConfigureAwait(false);
        LogCreated(logger, budget.Id, categoryId);
        return ToContract(budget);
    }

    public async Task<Budget> UpdateAsync(
        Guid owner, Guid budgetId, BudgetChanges changes, CancellationToken cancellationToken)
    {
        var current = await budgets.FindAsync(owner, budgetId, cancellationToken).ConfigureAwait(false)
            ?? throw new TransactionsNotFoundException(NotFound);

        var changed = BudgetRules.Apply(current, changes);
        // budgets.yaml: the result must still have an active category, changed or not.
        BudgetRules.RequireUsable(
            await categories.FindCategoryAsync(owner, changed.CategoryId, cancellationToken).ConfigureAwait(false));

        if (!await budgets.UpdateAsync(changed, cancellationToken).ConfigureAwait(false))
        {
            throw new TransactionsNotFoundException(NotFound);
        }

        LogChanged(logger, budgetId);
        return ToContract(changed);
    }

    public async Task DeleteAsync(Guid owner, Guid budgetId, CancellationToken cancellationToken)
    {
        if (!await budgets.DeleteAsync(owner, budgetId, cancellationToken).ConfigureAwait(false))
        {
            throw new TransactionsNotFoundException(NotFound);
        }

        LogDeleted(logger, budgetId);
    }

    private static Budget ToContract(BudgetRow budget) =>
        new(budget.Id, budget.CategoryId, budget.Amount, budget.PeriodStart, budget.PeriodEnd);

    [LoggerMessage(Level = LogLevel.Information, Message = "Budget {BudgetId} set for category {CategoryId}")]
    private static partial void LogCreated(ILogger logger, Guid budgetId, Guid categoryId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Budget {BudgetId} changed")]
    private static partial void LogChanged(ILogger logger, Guid budgetId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Budget {BudgetId} deleted")]
    private static partial void LogDeleted(ILogger logger, Guid budgetId);
}
