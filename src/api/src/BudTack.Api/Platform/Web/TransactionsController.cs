using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// <c>/api/v1/transactions</c> (HU-04, HU-05, HU-06, transactions.yaml). Every
/// action needs a signed-in user (NFR-05) and works on that user's rows only;
/// each maps one request to one facade call.
/// </summary>
[ApiController]
[Route("api/v1/transactions")]
internal sealed class TransactionsController(TransactionsFacade transactions) : ControllerBase
{
    [HttpGet]
    public Task<Page<Transaction>> List(int? page, int? limit, CancellationToken cancellationToken) =>
        transactions.ListTransactionsAsync(User.UserId(), page, limit, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<Transaction>> Record(NewTransaction request, CancellationToken cancellationToken)
    {
        var transaction = await transactions.RecordTransactionAsync(User.UserId(), request, cancellationToken);
        return Created($"/api/v1/transactions/{transaction.Id}", transaction);
    }

    [HttpDelete("{transactionId:guid}")]
    public async Task<NoContentResult> Delete(Guid transactionId, CancellationToken cancellationToken)
    {
        await transactions.DeleteTransactionAsync(User.UserId(), transactionId, cancellationToken);
        return NoContent();
    }
}
