using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Domain;

namespace BudTack.Identity.Internal.Application;

/// <summary>HU-09: the accounts, ordered by email, never with a password hash.</summary>
internal sealed class ListUsers(IUsersRepository users)
{
    public async Task<UserPage> HandleAsync(int? page, int? limit, CancellationToken cancellationToken)
    {
        var paging = UsersPaging.From(page, limit);
        var (rows, total) = await users.ListAsync(paging.Limit, paging.Offset, cancellationToken).ConfigureAwait(false);

        return new UserPage(
            [.. rows.Select(row => new Api.Contracts.User(row.Id, row.Name, row.Email, row.Role))],
            new UserPageMeta(paging.Page, paging.Limit, total));
    }
}
