using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// <c>/api/v1/categories</c> and their subcategories (HU-02, HU-03,
/// categories.yaml). Every action needs a signed-in user (NFR-05), every
/// write the ADMIN role (HU-12); each maps one request to one facade call.
/// </summary>
[ApiController]
[Route("api/v1/categories")]
internal sealed class CategoriesController(TransactionsFacade transactions) : ControllerBase
{
    [HttpGet]
    public Task<Page<Category>> List(int? page, int? limit, string? type, CancellationToken cancellationToken) =>
        transactions.ListCategoriesAsync(page, limit, type, cancellationToken);

    [Authorize(Policy = BearerAuthentication.AdminOnly)]
    [HttpPost]
    public async Task<ActionResult<Category>> Create(NewCategory request, CancellationToken cancellationToken)
    {
        var category = await transactions.CreateCategoryAsync(User.UserId(), request, cancellationToken);
        return Created($"/api/v1/categories/{category.Id}", category);
    }

    [Authorize(Policy = BearerAuthentication.AdminOnly)]
    [HttpDelete("{categoryId:guid}")]
    public async Task<NoContentResult> Delete(Guid categoryId, CancellationToken cancellationToken)
    {
        await transactions.DeleteCategoryAsync(categoryId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = BearerAuthentication.AdminOnly)]
    [HttpPatch("{categoryId:guid}")]
    public Task<Category> Update(Guid categoryId, CategoryChanges request, CancellationToken cancellationToken) =>
        transactions.UpdateCategoryAsync(User.UserId(), categoryId, request, cancellationToken);

    [HttpGet("{categoryId:guid}/subcategories")]
    public Task<Page<Subcategory>> ListSubcategories(
        Guid categoryId, int? page, int? limit, CancellationToken cancellationToken) =>
        transactions.ListSubcategoriesAsync(categoryId, page, limit, cancellationToken);

    [Authorize(Policy = BearerAuthentication.AdminOnly)]
    [HttpPost("{categoryId:guid}/subcategories")]
    public async Task<ActionResult<Subcategory>> CreateSubcategory(
        Guid categoryId, NewSubcategory request, CancellationToken cancellationToken)
    {
        var subcategory = await transactions.CreateSubcategoryAsync(User.UserId(), categoryId, request, cancellationToken);
        return Created($"/api/v1/categories/{categoryId}/subcategories/{subcategory.Id}", subcategory);
    }

    [Authorize(Policy = BearerAuthentication.AdminOnly)]
    [HttpDelete("{categoryId:guid}/subcategories/{subcategoryId:guid}")]
    public async Task<NoContentResult> DeleteSubcategory(
        Guid categoryId, Guid subcategoryId, CancellationToken cancellationToken)
    {
        await transactions.DeleteSubcategoryAsync(categoryId, subcategoryId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = BearerAuthentication.AdminOnly)]
    [HttpPatch("{categoryId:guid}/subcategories/{subcategoryId:guid}")]
    public Task<Subcategory> SetSubcategoryActive(
        Guid categoryId, Guid subcategoryId, ActivationUpdate request, CancellationToken cancellationToken) =>
        transactions.SetSubcategoryActiveAsync(User.UserId(), categoryId, subcategoryId, request, cancellationToken);
}
