namespace BudTack.Transactions.Internal.Domain;

/// <summary>
/// A recorded transaction of one owner (transactions.transactions) with its
/// lines. Named Movement in code so it never reads like a database transaction.
/// </summary>
internal sealed record Movement(
    Guid Id,
    Guid OwnedBy,
    DateOnly TransactionDate,
    string Description,
    IReadOnlyList<MovementLine> Lines,
    MovementDeletion? Deletion = null);

/// <summary>Who deleted a movement, when and why (BR-04).</summary>
internal sealed record MovementDeletion(DateTimeOffset DeletedAt, Guid DeletedBy, string Reason);
