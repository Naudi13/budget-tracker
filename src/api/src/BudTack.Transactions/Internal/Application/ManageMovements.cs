using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Domain;
using Microsoft.Extensions.Logging;

namespace BudTack.Transactions.Internal.Application;

/// <summary>
/// HU-04, HU-05 and HU-06: record a transaction with its lines (BR-01, BR-02,
/// BR-05), list the history newest first without the deleted ones, and delete
/// softly with a reason (BR-04). A USER reaches their own, an ADMIN all of them,
/// with who recorded each one. Descriptions, amounts and reasons are never logged,
/// only ids.
/// </summary>
internal sealed partial class ManageMovements(
    IMovementsRepository repository, IAccountNames accounts, ILogger<ManageMovements> logger)
{
    public const string NotFound = "The transaction does not exist.";

    private const string UnknownAccount = "Unknown";

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

    public Task<Page<Transaction>> ListAsync(
        Viewer viewer, int? page, int? limit, bool deleted, CancellationToken cancellationToken) =>
        ListAsync(viewer, page, limit, deleted, categoryId: null, cancellationToken);

    public async Task<Page<Transaction>> ListAsync(
        Viewer viewer, int? page, int? limit, bool deleted, Guid? categoryId, CancellationToken cancellationToken)
    {
        if (deleted && !viewer.IsAdmin)
        {
            throw new TransactionsForbiddenException();
        }

        var paging = Paging.From(page, limit);
        var (rows, total) = await repository.ListAsync(Scope(viewer), deleted, categoryId, paging, cancellationToken).ConfigureAwait(false);
        var names = viewer.IsAdmin ? await NamesAsync(rows, cancellationToken).ConfigureAwait(false) : null;
        return new Page<Transaction>(
            [.. rows.Select(row => names is null ? ToContract(row) : ToAdminContract(row, names))],
            new PageMeta(paging.Page, paging.Limit, total));
    }

    public Task<Totals> TotalsAsync(Viewer viewer, CancellationToken cancellationToken) =>
        TotalsAsync(viewer, categoryId: null, cancellationToken);

    public async Task<Totals> TotalsAsync(Viewer viewer, Guid? categoryId, CancellationToken cancellationToken)
    {
        var (income, expenses) = await repository.TotalsAsync(Scope(viewer), categoryId, cancellationToken).ConfigureAwait(false);
        return new Totals(income, expenses);
    }

    public async Task DeleteAsync(
        Viewer viewer, Guid transactionId, TransactionDeletion request, CancellationToken cancellationToken)
    {
        var reason = MovementRules.Validate(request);
        if (!await repository.SoftDeleteAsync(viewer.UserId, Scope(viewer), transactionId, reason, cancellationToken).ConfigureAwait(false))
        {
            throw new TransactionsNotFoundException(NotFound);
        }

        LogDeleted(logger, transactionId);
    }

    private static Guid? Scope(Viewer viewer) => viewer.IsAdmin ? null : viewer.UserId;

    private async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        IReadOnlyList<Movement> rows, CancellationToken cancellationToken)
    {
        var ids = rows.Select(row => row.OwnedBy)
            .Concat(rows.Where(row => row.Deletion is not null).Select(row => row.Deletion!.DeletedBy))
            .Distinct()
            .ToList();
        return await accounts.NamesOfAsync(ids, cancellationToken).ConfigureAwait(false);
    }

    private static Transaction ToAdminContract(Movement movement, IReadOnlyDictionary<Guid, string> names) =>
        ToContract(movement) with
        {
            RecordedBy = names.GetValueOrDefault(movement.OwnedBy, UnknownAccount),
            Deletion = movement.Deletion is { } deletion
                ? new DeletionRecord(deletion.DeletedAt, names.GetValueOrDefault(deletion.DeletedBy, UnknownAccount), deletion.Reason)
                : null,
        };

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
