namespace BudTack.Transactions.Api.Contracts;

/// <summary>The body of <c>POST /categories/{categoryId}/subcategories</c> (categories.yaml <c>NewSubcategory</c>).</summary>
public sealed record NewSubcategory(string? Name);
