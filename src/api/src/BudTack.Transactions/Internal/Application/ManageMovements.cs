using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Domain;
using Microsoft.Extensions.Logging;

namespace BudTack.Transactions.Internal.Application;

/// <summary>
/// HU-04, HU-05 and HU-06: record a transaction with its lines (BR-01, BR-02,
/// BR-05), list the history newest first without the deleted ones, and delete
/// softly (BR-04). Always for one owner. Descriptions and amounts are never
/// logged, only ids.
/// </summary>
internal sealed partial class ManageMovements(IMovementsRepository repository, ILogger<ManageMovements> logger)
{
    public const string NotFound = "The transaction does not exist.";

    public async Task<Transaction> RecordAsync(Guid owner, NewTransaction request, CancellationToken cancellationToken)
    {
        var (date, description, lines) = MovementRules.Validate(request);
        var ids = lines.Select(line => line.SubcategoryId).Distinct().ToList();
        var uses = await repository.FindSubcategoryUsesAsync(ids, cancellationToken).ConfigureAwait(false);
        var classified = MovementRules.Classify(lines, uses);

        var movement = new Movement(Guid.CreateVersion7(), owner, date, description, classified);
        await repository.AddAsync(movement, cancellationToken).ConfigureAwait(false);
        LogRecorded(logger, movement.Id, classified.Count, classified[0].Type);
        return ToContract(movement);
    }

    public async Task<Page<Transaction>> ListAsync(Guid owner, int? page, int? limit, CancellationToken cancellationToken)
    {
        var paging = Paging.From(page, limit);
        var (rows, total) = await repository.ListAsync(owner, paging, cancellationToken).ConfigureAwait(false);
        return new Page<Transaction>([.. rows.Select(ToContract)], new PageMeta(paging.Page, paging.Limit, total));
    }

    public async Task<Totals> TotalsAsync(Guid owner, CancellationToken cancellationToken)
    {
        var (income, expenses) = await repository.TotalsAsync(owner, cancellationToken).ConfigureAwait(false);
        return new Totals(income, expenses);
    }

    public async Task DeleteAsync(Guid owner, Guid transactionId, CancellationToken cancellationToken)
    {
        if (!await repository.SoftDeleteAsync(owner, transactionId, cancellationToken).ConfigureAwait(false))
        {
            throw new TransactionsNotFoundException(NotFound);
        }

        LogDeleted(logger, transactionId);
    }

    private static Transaction ToContract(Movement movement) =>
        new(
            movement.Id,
            movement.TransactionDate,
            movement.Description,
            [.. movement.Lines.Select(line => new TransactionDetail(line.Id, line.SubcategoryId, line.Amount, line.Type))]);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transaction {TransactionId} recorded: {Lines} {Type} line(s)")]
    private static partial void LogRecorded(ILogger logger, Guid transactionId, int lines, string type);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transaction {TransactionId} deleted (soft)")]
    private static partial void LogDeleted(ILogger logger, Guid transactionId);
}
