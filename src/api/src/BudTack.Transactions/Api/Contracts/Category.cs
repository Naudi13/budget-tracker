namespace BudTack.Transactions.Api.Contracts;

/// <summary>A category as the API returns it (categories.yaml <c>Category</c>); <c>Type</c> is <c>income</c> or <c>expense</c>.</summary>
public sealed record Category(Guid Id, string Name, string Type, bool IsActive);
