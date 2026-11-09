using BudTack.Identity.Internal.Domain;

namespace BudTack.Identity.Internal.Application;

/// <summary>identity.users, as the use cases need it. Emails arrive already normalized.</summary>
internal interface IUsersRepository
{
    /// <summary>Stores a new account; throws <c>EmailAlreadyInUseException</c> when the email is taken.</summary>
    Task AddAsync(User user, Guid actor, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>One page of accounts ordered by email, and how many there are in all.</summary>
    Task<(IReadOnlyList<User> Rows, long Total)> ListAsync(int limit, int offset, CancellationToken cancellationToken);
}
