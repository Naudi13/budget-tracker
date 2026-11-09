namespace BudTack.Transactions.Internal.Domain;

/// <summary>A category of the business (transactions.categories, ADR-009).</summary>
internal sealed record Category(Guid Id, string Name, string Type, bool IsActive);
