using BudTack.Identity.Internal.Domain;

namespace BudTack.Identity.Internal.Application;

/// <summary>identity.users, as the use cases need it. Emails arrive already normalized.</summary>
internal interface IUsersRepository
{
    /// <summary>Stores a new account; throws <c>EmailAlreadyInUseException</c> when the email is taken.</summary>
    Task AddAsync(User user, Guid actor, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>True only for an account that exists and is active; read on every request (ADR-011).</summary>
    Task<bool> IsActiveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Changes the state, and when inactivating cancels the unused links, in one step; null when there is no such account.</summary>
    Task<User?> SetActiveAsync(Guid actor, Guid id, bool isActive, CancellationToken cancellationToken);

    /// <summary>Removes an account that has no password, with its links; false when it has one.</summary>
    Task<bool> DeleteNeverActivatedAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>One page of accounts ordered by email, and how many there are in all.</summary>
    Task<(IReadOnlyList<User> Rows, long Total)> ListAsync(int limit, int offset, CancellationToken cancellationToken);
}
