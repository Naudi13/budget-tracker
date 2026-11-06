using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// <c>/api/v1/categories</c> and their subcategories (HU-02, HU-03,
/// categories.yaml). Every action needs a signed-in user (NFR-05) and works on
/// that user's rows only; each maps one request to one facade call.
/// </summary>
[ApiController]
[Route("api/v1/categories")]
internal sealed class CategoriesController(TransactionsFacade transactions) : ControllerBase
{
    [HttpGet]
    public Task<Page<Category>> List(int? page, int? limit, string? type, CancellationToken cancellationToken) =>
        transactions.ListCategoriesAsync(User.UserId(), page, limit, type, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<Category>> Create(NewCategory request, CancellationToken cancellationToken)
    {
        var category = await transactions.CreateCategoryAsync(User.UserId(), request, cancellationToken);
        return Created($"/api/v1/categories/{category.Id}", category);
    }

    [HttpDelete("{categoryId:guid}")]
    public async Task<NoContentResult> Delete(Guid categoryId, CancellationToken cancellationToken)
    {
        await transactions.DeleteCategoryAsync(User.UserId(), categoryId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{categoryId:guid}")]
    public Task<Category> SetActive(Guid categoryId, ActivationUpdate request, CancellationToken cancellationToken) =>
        transactions.SetCategoryActiveAsync(User.UserId(), categoryId, request, cancellationToken);

    [HttpGet("{categoryId:guid}/subcategories")]
    public Task<Page<Subcategory>> ListSubcategories(
        Guid categoryId, int? page, int? limit, CancellationToken cancellationToken) =>
        transactions.ListSubcategoriesAsync(User.UserId(), categoryId, page, limit, cancellationToken);

    [HttpPost("{categoryId:guid}/subcategories")]
    public async Task<ActionResult<Subcategory>> CreateSubcategory(
        Guid categoryId, NewSubcategory request, CancellationToken cancellationToken)
    {
        var subcategory = await transactions.CreateSubcategoryAsync(User.UserId(), categoryId, request, cancellationToken);
        return Created($"/api/v1/categories/{categoryId}/subcategories/{subcategory.Id}", subcategory);
    }

    [HttpDelete("{categoryId:guid}/subcategories/{subcategoryId:guid}")]
    public async Task<NoContentResult> DeleteSubcategory(
        Guid categoryId, Guid subcategoryId, CancellationToken cancellationToken)
    {
        await transactions.DeleteSubcategoryAsync(User.UserId(), categoryId, subcategoryId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{categoryId:guid}/subcategories/{subcategoryId:guid}")]
    public Task<Subcategory> SetSubcategoryActive(
        Guid categoryId, Guid subcategoryId, ActivationUpdate request, CancellationToken cancellationToken) =>
        transactions.SetSubcategoryActiveAsync(User.UserId(), categoryId, subcategoryId, request, cancellationToken);
}
