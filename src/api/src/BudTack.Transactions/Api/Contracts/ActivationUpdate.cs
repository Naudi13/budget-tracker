namespace BudTack.Transactions.Api.Contracts;

/// <summary>The body of the <c>PATCH</c> that activates or deactivates a category or subcategory (BR-05).</summary>
public sealed record ActivationUpdate(bool? IsActive);
