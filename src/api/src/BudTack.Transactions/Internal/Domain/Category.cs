namespace BudTack.Transactions.Internal.Domain;

/// <summary>A category of one owner (transactions.categories).</summary>
internal sealed record Category(Guid Id, Guid OwnedBy, string Name, string Type, bool IsActive);
