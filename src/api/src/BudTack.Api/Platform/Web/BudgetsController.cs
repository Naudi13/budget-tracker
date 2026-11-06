using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// <c>/api/v1/budgets</c> (HU-07, budgets.yaml). Every action needs a signed-in
/// user (NFR-05) and works on that user's rows only; each maps one request to
/// one facade call.
/// </summary>
[ApiController]
[Route("api/v1/budgets")]
internal sealed class BudgetsController(TransactionsFacade transactions) : ControllerBase
{
    [HttpGet]
    public Task<Page<Budget>> List(int? page, int? limit, Guid? categoryId, CancellationToken cancellationToken) =>
        transactions.ListBudgetsAsync(User.UserId(), page, limit, categoryId, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<Budget>> Create(NewBudget request, CancellationToken cancellationToken)
    {
        var budget = await transactions.CreateBudgetAsync(User.UserId(), request, cancellationToken);
        return Created($"/api/v1/budgets/{budget.Id}", budget);
    }

    [HttpPatch("{budgetId:guid}")]
    public Task<Budget> Update(Guid budgetId, BudgetChanges request, CancellationToken cancellationToken) =>
        transactions.UpdateBudgetAsync(User.UserId(), budgetId, request, cancellationToken);

    [HttpDelete("{budgetId:guid}")]
    public async Task<NoContentResult> Delete(Guid budgetId, CancellationToken cancellationToken)
    {
        await transactions.DeleteBudgetAsync(User.UserId(), budgetId, cancellationToken);
        return NoContent();
    }
}
