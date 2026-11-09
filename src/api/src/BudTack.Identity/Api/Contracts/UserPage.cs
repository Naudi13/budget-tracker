namespace BudTack.Identity.Api.Contracts;

/// <summary>One page of accounts: <c>{ data, meta }</c> (identity.yaml <c>UserPage</c>).</summary>
public sealed record UserPage(IReadOnlyList<User> Data, UserPageMeta Meta);

/// <summary>Where a page sits in the whole list (common.yaml <c>PaginationMeta</c>).</summary>
public sealed record UserPageMeta(int Page, int Limit, long Total);
