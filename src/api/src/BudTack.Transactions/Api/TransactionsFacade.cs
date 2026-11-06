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
/// (400), <see cref="TransactionsNotFoundException"/> (404) and
/// <see cref="TransactionsConflictException"/> (409).
/// </summary>
public sealed class TransactionsFacade
{
    private readonly ManageCategories _categories;

    internal TransactionsFacade(IdentityFacade identity, ManageCategories categories)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
        _categories = categories;
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
}
