using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Domain;
using Microsoft.Extensions.Logging;

namespace BudTack.Transactions.Internal.Application;

/// <summary>
/// HU-07: list, set, change and delete the business's budgets (ADR-009). A
/// new budget and a changed one are held to the same invariants (BR-06), and only
/// an active category can be budgeted (BR-05). The database checks
/// the amount and the period again, so no path can store a broken row.
/// </summary>
internal sealed partial class ManageBudgets(
    IBudgetsRepository budgets, ICategoriesRepository categories, TimeProvider clock, ILogger<ManageBudgets> logger)
{
    public const string NotFound = "The budget does not exist.";

    public async Task<Page<Budget>> ListAsync(
        int? page, int? limit, Guid? categoryId, CancellationToken cancellationToken)
    {
        var paging = Paging.From(page, limit);
        var (rows, total) = await budgets.ListAsync(categoryId, paging, cancellationToken).ConfigureAwait(false);
        return new Page<Budget>([.. rows.Select(ToContract)], new PageMeta(paging.Page, paging.Limit, total));
    }

    /// <summary>
    /// HU-11, BR-08: budget, spent and available for today, or null when no budget
    /// is in force. An unknown category is not found.
    /// </summary>
    public async Task<BudgetProgress?> ProgressAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is { } id
            && await categories.FindCategoryAsync(id, cancellationToken).ConfigureAwait(false) is null)
        {
            throw new TransactionsNotFoundException(ManageCategories.CategoryNotFound);
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var (count, amount, spent) = await budgets.ProgressAsync(categoryId, today, cancellationToken).ConfigureAwait(false);
        return count == 0 ? null : new BudgetProgress(amount, spent, amount - spent);
    }

    public async Task<Budget> CreateAsync(Guid actor, NewBudget request, CancellationToken cancellationToken)
    {
        var (categoryId, amount, start, end) = BudgetRules.Validate(request);
        BudgetRules.RequireUsable(await categories.FindCategoryAsync(categoryId, cancellationToken).ConfigureAwait(false));

        var budget = new BudgetRow(Guid.CreateVersion7(), categoryId, amount, start, end);
        await budgets.AddAsync(budget, actor, cancellationToken).ConfigureAwait(false);
        LogCreated(logger, budget.Id, categoryId);
        return ToContract(budget);
    }

    public async Task<Budget> UpdateAsync(
        Guid actor, Guid budgetId, BudgetChanges changes, CancellationToken cancellationToken)
    {
        var current = await budgets.FindAsync(budgetId, cancellationToken).ConfigureAwait(false)
            ?? throw new TransactionsNotFoundException(NotFound);

        var changed = BudgetRules.Apply(current, changes);
        // budgets.yaml: the result must still have an active category, changed or not.
        BudgetRules.RequireUsable(
            await categories.FindCategoryAsync(changed.CategoryId, cancellationToken).ConfigureAwait(false));

        if (!await budgets.UpdateAsync(changed, actor, cancellationToken).ConfigureAwait(false))
        {
            throw new TransactionsNotFoundException(NotFound);
        }

        LogChanged(logger, budgetId);
        return ToContract(changed);
    }

    public async Task DeleteAsync(Guid budgetId, CancellationToken cancellationToken)
    {
        if (!await budgets.DeleteAsync(budgetId, cancellationToken).ConfigureAwait(false))
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
