using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// <c>/api/v1/transactions</c> (HU-04, HU-05, HU-06, transactions.yaml). Every
/// action needs a signed-in user (NFR-05). A USER reaches their own rows, an ADMIN
/// all of them; each maps one request to one facade call.
/// </summary>
[ApiController]
[Route("api/v1/transactions")]
internal sealed class TransactionsController(TransactionsFacade transactions) : ControllerBase
{
    [HttpGet]
    public Task<Page<Transaction>> List(int? page, int? limit, bool? deleted, CancellationToken cancellationToken) =>
        transactions.ListTransactionsAsync(User.Viewer(), page, limit, deleted ?? false, cancellationToken: cancellationToken);

    [HttpPost]
    public async Task<ActionResult<Transaction>> Record(NewTransaction request, CancellationToken cancellationToken)
    {
        var transaction = await transactions.RecordTransactionAsync(User.UserId(), request, cancellationToken);
        return Created($"/api/v1/transactions/{transaction.Id}", transaction);
    }

    [HttpPost("{transactionId:guid}/deletion")]
    public async Task<NoContentResult> Delete(
        Guid transactionId, TransactionDeletion request, CancellationToken cancellationToken)
    {
        await transactions.DeleteTransactionAsync(User.Viewer(), transactionId, request, cancellationToken);
        return NoContent();
    }
}
