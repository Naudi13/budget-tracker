using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Domain;
using Microsoft.Extensions.Logging;
using DomainCategory = BudTack.Transactions.Internal.Domain.Category;
using DomainSubcategory = BudTack.Transactions.Internal.Domain.Subcategory;

namespace BudTack.Transactions.Internal.Application;

/// <summary>
/// HU-02 and HU-03: list, create, delete, and activate or deactivate categories
/// and their subcategories, always for one owner. Uniqueness and BR-05 are
/// decided by the database (unique indexes, ON DELETE RESTRICT), so two requests
/// at once cannot get around them.
/// </summary>
internal sealed partial class ManageCategories(ICategoriesRepository repository, ILogger<ManageCategories> logger)
{
    public const string CategoryNotFound = "The category does not exist.";
    public const string SubcategoryNotFound = "The subcategory does not exist.";

    public async Task<Page<Api.Contracts.Category>> ListCategoriesAsync(
        Guid owner, int? page, int? limit, string? type, CancellationToken cancellationToken)
    {
        var paging = CategoryRules.Validate(page, limit);
        var filter = CategoryRules.ValidateFilter(type);
        var (rows, total) = await repository.ListCategoriesAsync(owner, filter, paging, cancellationToken).ConfigureAwait(false);
        return new Page<Api.Contracts.Category>([.. rows.Select(ToContract)], new PageMeta(paging.Page, paging.Limit, total));
    }

    public async Task<Api.Contracts.Category> CreateCategoryAsync(
        Guid owner, NewCategory request, CancellationToken cancellationToken)
    {
        var (name, type) = CategoryRules.Validate(request);
        var category = new DomainCategory(Guid.CreateVersion7(), owner, name, type, IsActive: true);
        await repository.AddCategoryAsync(category, cancellationToken).ConfigureAwait(false);
        LogCategoryCreated(logger, category.Id, category.Type);
        return ToContract(category);
    }

    public async Task DeleteCategoryAsync(Guid owner, Guid categoryId, CancellationToken cancellationToken)
    {
        if (!await repository.DeleteCategoryAsync(owner, categoryId, cancellationToken).ConfigureAwait(false))
        {
            throw new TransactionsNotFoundException(CategoryNotFound);
        }

        LogCategoryDeleted(logger, categoryId);
    }

    public async Task<Api.Contracts.Category> SetCategoryActiveAsync(
        Guid owner, Guid categoryId, ActivationUpdate request, CancellationToken cancellationToken)
    {
        var isActive = CategoryRules.Validate(request);
        var category = await repository.SetCategoryActiveAsync(owner, categoryId, isActive, cancellationToken).ConfigureAwait(false)
            ?? throw new TransactionsNotFoundException(CategoryNotFound);
        LogCategoryActivation(logger, categoryId, isActive);
        return ToContract(category);
    }

    public async Task<Page<Api.Contracts.Subcategory>> ListSubcategoriesAsync(
        Guid owner, Guid categoryId, int? page, int? limit, CancellationToken cancellationToken)
    {
        var paging = CategoryRules.Validate(page, limit);
        await RequireCategoryAsync(owner, categoryId, cancellationToken).ConfigureAwait(false);
        var (rows, total) = await repository.ListSubcategoriesAsync(categoryId, paging, cancellationToken).ConfigureAwait(false);
        return new Page<Api.Contracts.Subcategory>([.. rows.Select(ToContract)], new PageMeta(paging.Page, paging.Limit, total));
    }

    public async Task<Api.Contracts.Subcategory> CreateSubcategoryAsync(
        Guid owner, Guid categoryId, NewSubcategory request, CancellationToken cancellationToken)
    {
        var name = CategoryRules.Validate(request);
        await RequireCategoryAsync(owner, categoryId, cancellationToken).ConfigureAwait(false);
        var subcategory = new DomainSubcategory(Guid.CreateVersion7(), categoryId, name, IsActive: true);
        await repository.AddSubcategoryAsync(subcategory, owner, cancellationToken).ConfigureAwait(false);
        LogSubcategoryCreated(logger, subcategory.Id, categoryId);
        return ToContract(subcategory);
    }

    public async Task DeleteSubcategoryAsync(
        Guid owner, Guid categoryId, Guid subcategoryId, CancellationToken cancellationToken)
    {
        if (!await repository.DeleteSubcategoryAsync(owner, categoryId, subcategoryId, cancellationToken).ConfigureAwait(false))
        {
            throw new TransactionsNotFoundException(SubcategoryNotFound);
        }

        LogSubcategoryDeleted(logger, subcategoryId);
    }

    public async Task<Api.Contracts.Subcategory> SetSubcategoryActiveAsync(
        Guid owner, Guid categoryId, Guid subcategoryId, ActivationUpdate request, CancellationToken cancellationToken)
    {
        var isActive = CategoryRules.Validate(request);
        var subcategory = await repository
                .SetSubcategoryActiveAsync(owner, categoryId, subcategoryId, isActive, cancellationToken)
                .ConfigureAwait(false)
            ?? throw new TransactionsNotFoundException(SubcategoryNotFound);
        LogSubcategoryActivation(logger, subcategoryId, isActive);
        return ToContract(subcategory);
    }

    private async Task RequireCategoryAsync(Guid owner, Guid categoryId, CancellationToken cancellationToken)
    {
        if (await repository.FindCategoryAsync(owner, categoryId, cancellationToken).ConfigureAwait(false) is null)
        {
            throw new TransactionsNotFoundException(CategoryNotFound);
        }
    }

    private static Api.Contracts.Category ToContract(DomainCategory category) =>
        new(category.Id, category.Name, category.Type, category.IsActive);

    private static Api.Contracts.Subcategory ToContract(DomainSubcategory subcategory) =>
        new(subcategory.Id, subcategory.CategoryId, subcategory.Name, subcategory.IsActive);

    [LoggerMessage(Level = LogLevel.Information, Message = "Category {CategoryId} created ({Type})")]
    private static partial void LogCategoryCreated(ILogger logger, Guid categoryId, string type);

    [LoggerMessage(Level = LogLevel.Information, Message = "Category {CategoryId} deleted")]
    private static partial void LogCategoryDeleted(ILogger logger, Guid categoryId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Category {CategoryId} active: {IsActive}")]
    private static partial void LogCategoryActivation(ILogger logger, Guid categoryId, bool isActive);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subcategory {SubcategoryId} created in category {CategoryId}")]
    private static partial void LogSubcategoryCreated(ILogger logger, Guid subcategoryId, Guid categoryId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subcategory {SubcategoryId} deleted")]
    private static partial void LogSubcategoryDeleted(ILogger logger, Guid subcategoryId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Subcategory {SubcategoryId} active: {IsActive}")]
    private static partial void LogSubcategoryActivation(ILogger logger, Guid subcategoryId, bool isActive);
}
