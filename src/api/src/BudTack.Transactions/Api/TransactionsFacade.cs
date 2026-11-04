using BudTack.Identity.Api;

namespace BudTack.Transactions.Api;

/// <summary>
/// The only entry point into the transactions module. Its methods arrive with
/// their stories (HU-03 to HU-07); until then the facade exists so the host,
/// the dashboard and the boundary tests can depend on it.
/// </summary>
public sealed class TransactionsFacade
{
    internal TransactionsFacade(IdentityFacade identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
    }

    /// <summary>Attributes the audit columns of every write (BR-04).</summary>
    internal IdentityFacade Identity { get; }
}
