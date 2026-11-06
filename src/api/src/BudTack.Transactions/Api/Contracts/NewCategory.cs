namespace BudTack.Transactions.Api.Contracts;

/// <summary>The body of <c>POST /categories</c> (categories.yaml <c>NewCategory</c>). Fields are nullable: the module says what is missing.</summary>
public sealed record NewCategory(string? Name, string? Type);
