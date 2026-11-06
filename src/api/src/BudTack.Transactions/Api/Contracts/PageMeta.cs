namespace BudTack.Transactions.Api.Contracts;

/// <summary>Where a page sits in the whole list (common.yaml <c>PaginationMeta</c>).</summary>
public sealed record PageMeta(int Page, int Limit, long Total);
