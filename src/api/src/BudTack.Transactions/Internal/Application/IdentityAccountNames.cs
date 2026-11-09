using BudTack.Identity.Api;

namespace BudTack.Transactions.Internal.Application;

/// <summary>Names through identity's public API, the only way into another module.</summary>
internal sealed class IdentityAccountNames(IdentityFacade identity) : IAccountNames
{
    public Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        identity.NamesOfAsync(userIds, cancellationToken);
}
