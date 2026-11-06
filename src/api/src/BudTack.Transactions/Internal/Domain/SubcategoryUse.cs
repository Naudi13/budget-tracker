namespace BudTack.Transactions.Internal.Domain;

/// <summary>
/// What a new line needs to know about its subcategory: its category's type
/// (BR-01), and whether both are active (BR-05).
/// </summary>
internal sealed record SubcategoryUse(Guid SubcategoryId, string Type, bool IsUsable);
