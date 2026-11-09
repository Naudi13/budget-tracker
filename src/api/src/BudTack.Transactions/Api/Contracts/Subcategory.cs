namespace BudTack.Transactions.Api.Contracts;

/// <summary>A subcategory as the API returns it (categories.yaml <c>Subcategory</c>).</summary>
public sealed record Subcategory(Guid Id, Guid CategoryId, string Name, bool IsActive);
