namespace BudTack.Transactions.Internal.Domain;

/// <summary>
/// A recorded transaction of one owner (transactions.transactions) with its
/// lines. Named Movement in code so it never reads like a database transaction.
/// </summary>
internal sealed record Movement(
    Guid Id, Guid OwnedBy, DateOnly TransactionDate, string Description, IReadOnlyList<MovementLine> Lines);
