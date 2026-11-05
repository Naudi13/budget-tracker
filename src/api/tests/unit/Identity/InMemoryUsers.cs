using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Application;

namespace BudTack.Unit.Tests.Identity;

/// <summary>identity.users in memory, with the unique email index's behaviour.</summary>
internal sealed class InMemoryUsers : IUsersRepository
{
    public List<BudTack.Identity.Internal.Domain.User> Rows { get; } = [];

    public Task AddAsync(BudTack.Identity.Internal.Domain.User user, CancellationToken cancellationToken)
    {
        if (Rows.Exists(row => row.Email == user.Email))
        {
            throw new EmailAlreadyInUseException();
        }

        Rows.Add(user);
        return Task.CompletedTask;
    }

    public Task<BudTack.Identity.Internal.Domain.User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Find(row => row.Email == email));
}
