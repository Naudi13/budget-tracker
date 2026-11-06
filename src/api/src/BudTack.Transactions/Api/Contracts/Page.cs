namespace BudTack.Transactions.Api.Contracts;

/// <summary>One page of a list: <c>{ data, meta }</c> (07-api/rest-conventions.md, Pagination).</summary>
public sealed record Page<T>(IReadOnlyList<T> Data, PageMeta Meta);
