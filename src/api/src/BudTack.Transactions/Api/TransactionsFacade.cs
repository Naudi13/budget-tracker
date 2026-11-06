using BudTack.Identity.Api;
using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Application;

namespace BudTack.Transactions.Api;

/// <summary>
/// The only entry point into the transactions module (09-modules/transactions).
/// Every method takes the signed-in user's id, which the host got from
/// <see cref="IdentityFacade"/>: a user reads and changes only their own rows,
/// and anyone else's answer <see cref="TransactionsNotFoundException"/>.
/// Errors a caller can expect are <see cref="TransactionsValidationException"/>
/// (400), <see cref="TransactionsNotFoundException"/> (404),
/// <see cref="TransactionsConflictException"/> (409) and
/// <see cref="TransactionsBusinessRuleException"/> (422).
/// </summary>
public sealed class TransactionsFacade
{
    private readonly ManageCategories _categories;
    private readonly ManageMovements _movements;
    private readonly ManageBudgets _budgets;

    internal TransactionsFacade(
        IdentityFacade identity, ManageCategories categories, ManageMovements movements, ManageBudgets budgets)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
        _categories = categories;
        _movements = movements;
        _budgets = budgets;
    }

    /// <summary>Identity's public API, for what transactions needs to know about users.</summary>
    internal IdentityFacade Identity { get; }

    /// <summary>HU-02: <c>GET /categories</c>, optionally only <c>income</c> or <c>expense</c>.</summary>
    public Task<Page<Category>> ListCategoriesAsync(
        Guid userId, int? page = null, int? limit = null, string? type = null, CancellationToken cancellationToken = default) =>
        _categories.ListCategoriesAsync(userId, page, limit, type, cancellationToken);

    /// <summary>HU-02: <c>POST /categories</c>; a name already used in that type is a conflict.</summary>
    public Task<Category> CreateCategoryAsync(Guid userId, NewCategory request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _categories.CreateCategoryAsync(userId, request, cancellationToken);
    }

    /// <summary>HU-03: <c>DELETE /categories/{id}</c>; refused while it owns a subcategory or a budget (BR-05).</summary>
    public Task DeleteCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default) =>
        _categories.DeleteCategoryAsync(userId, categoryId, cancellationToken);

    /// <summary>HU-03: <c>PATCH /categories/{id}</c> with <c>isActive</c> (BR-05).</summary>
    public Task<Category> SetCategoryActiveAsync(
        Guid userId, Guid categoryId, ActivationUpdate request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _categories.SetCategoryActiveAsync(userId, categoryId, request, cancellationToken);
    }

    /// <summary>HU-02: <c>GET /categories/{id}/subcategories</c>.</summary>
    public Task<Page<Subcategory>> ListSubcategoriesAsync(
        Guid userId, Guid categoryId, int? page = null, int? limit = null, CancellationToken cancellationToken = default) =>
        _categories.ListSubcategoriesAsync(userId, categoryId, page, limit, cancellationToken);

    /// <summary>HU-02: <c>POST /categories/{id}/subcategories</c>; a name already used in that category is a conflict.</summary>
    public Task<Subcategory> CreateSubcategoryAsync(
        Guid userId, Guid categoryId, NewSubcategory request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _categories.CreateSubcategoryAsync(userId, categoryId, request, cancellationToken);
    }

    /// <summary>HU-03: <c>DELETE /categories/{id}/subcategories/{id}</c>; refused while a transaction uses it (BR-05).</summary>
    public Task DeleteSubcategoryAsync(
        Guid userId, Guid categoryId, Guid subcategoryId, CancellationToken cancellationToken = default) =>
        _categories.DeleteSubcategoryAsync(userId, categoryId, subcategoryId, cancellationToken);

    /// <summary>HU-03: <c>PATCH /categories/{id}/subcategories/{id}</c> with <c>isActive</c> (BR-05).</summary>
    public Task<Subcategory> SetSubcategoryActiveAsync(
        Guid userId, Guid categoryId, Guid subcategoryId, ActivationUpdate request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _categories.SetSubcategoryActiveAsync(userId, categoryId, subcategoryId, request, cancellationToken);
    }

    /// <summary>
    /// HU-04: <c>POST /transactions</c>. Each line's type comes from its subcategory
    /// (BR-01); lines of both types, or of an inactive subcategory or category, are
    /// refused with <see cref="TransactionsBusinessRuleException"/> (BR-02, BR-05).
    /// </summary>
    public Task<Transaction> RecordTransactionAsync(Guid userId, NewTransaction request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _movements.RecordAsync(userId, request, cancellationToken);
    }

    /// <summary>HU-05: <c>GET /transactions</c>, newest first, without deleted ones (BR-04).</summary>
    public Task<Page<Transaction>> ListTransactionsAsync(
        Guid userId, int? page = null, int? limit = null, CancellationToken cancellationToken = default) =>
        _movements.ListAsync(userId, page, limit, cancellationToken);

    /// <summary>HU-06: <c>DELETE /transactions/{id}</c>, a soft delete (BR-04); an already deleted one is not found.</summary>
    public Task DeleteTransactionAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default) =>
        _movements.DeleteAsync(userId, transactionId, cancellationToken);

    /// <summary>
    /// HU-08: the owner's income and expense totals, computed on read (BR-03)
    /// without deleted transactions (BR-04). Called by the dashboard.
    /// </summary>
    public Task<Totals> GetTotalsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _movements.TotalsAsync(userId, cancellationToken);

    /// <summary>HU-07: <c>GET /budgets</c>, newest period first, optionally for one category.</summary>
    public Task<Page<Budget>> ListBudgetsAsync(
        Guid userId, int? page = null, int? limit = null, Guid? categoryId = null, CancellationToken cancellationToken = default) =>
        _budgets.ListAsync(userId, page, limit, categoryId, cancellationToken);

    /// <summary>
    /// HU-07: <c>POST /budgets</c>. A period that does not end after it starts, or
    /// an inactive category (BR-05), is a <see cref="TransactionsBusinessRuleException"/>.
    /// </summary>
    public Task<Budget> CreateBudgetAsync(Guid userId, NewBudget request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _budgets.CreateAsync(userId, request, cancellationToken);
    }

    /// <summary>HU-07: <c>PATCH /budgets/{id}</c>; the changed budget is held to the same rules (BR-06).</summary>
    public Task<Budget> UpdateBudgetAsync(
        Guid userId, Guid budgetId, BudgetChanges changes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);
        return _budgets.UpdateAsync(userId, budgetId, changes, cancellationToken);
    }

    /// <summary>HU-07: <c>DELETE /budgets/{id}</c>, a physical delete (BR-06).</summary>
    public Task DeleteBudgetAsync(Guid userId, Guid budgetId, CancellationToken cancellationToken = default) =>
        _budgets.DeleteAsync(userId, budgetId, cancellationToken);
}
