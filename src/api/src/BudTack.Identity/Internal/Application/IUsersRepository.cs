using BudTack.Identity.Internal.Domain;

namespace BudTack.Identity.Internal.Application;

/// <summary>identity.users, as the use cases need it. Emails arrive already normalized.</summary>
internal interface IUsersRepository
{
    /// <summary>Stores a new account; throws <c>EmailAlreadyInUseException</c> when the email is taken.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}
