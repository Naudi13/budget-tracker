namespace BudTack.Transactions.Api.Contracts;

/// <summary>The body of <c>PATCH /categories/{id}</c> (categories.yaml <c>CategoryChanges</c>): only the fields to change.</summary>
public sealed record CategoryChanges(string? Name, bool? IsActive);
