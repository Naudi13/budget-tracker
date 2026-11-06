namespace BudTack.Transactions.Internal.Domain;

/// <summary>A subcategory; it belongs to its category's owner (transactions.subcategories).</summary>
internal sealed record Subcategory(Guid Id, Guid CategoryId, string Name, bool IsActive);
