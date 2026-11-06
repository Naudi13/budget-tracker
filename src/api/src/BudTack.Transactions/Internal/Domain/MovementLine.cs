namespace BudTack.Transactions.Internal.Domain;

/// <summary>
/// One line of a movement (transactions.transaction_details). <c>Type</c> is
/// never stored on the line: it is read from the subcategory's category (BR-01).
/// </summary>
internal sealed record MovementLine(Guid Id, Guid SubcategoryId, decimal Amount, string Type);
