using BudTack.Identity.Api.Contracts;
using BudTack.Identity.Internal.Application;

namespace BudTack.Unit.Tests.Identity;

/// <summary>identity.users in memory, with the unique email index's behaviour.</summary>
internal sealed class InMemoryUsers : IUsersRepository
{
    public List<BudTack.Identity.Internal.Domain.User> Rows { get; } = [];

    public Task AddAsync(BudTack.Identity.Internal.Domain.User user, Guid actor, CancellationToken cancellationToken)
    {
        if (Rows.Exists(row => row.Email == user.Email))
        {
            throw new EmailAlreadyInUseException();
        }

        Rows.Add(user);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(
            Rows.Where(row => ids.Contains(row.Id)).ToDictionary(row => row.Id, row => row.Name));

    public Task<bool> IsActiveAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Exists(row => row.Id == id && row.IsActive));

    public Task<BudTack.Identity.Internal.Domain.User?> SetActiveAsync(
        Guid actor, Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var index = Rows.FindIndex(row => row.Id == id);
        if (index < 0)
        {
            return Task.FromResult<BudTack.Identity.Internal.Domain.User?>(null);
        }

        Rows[index] = Rows[index] with { IsActive = isActive };
        return Task.FromResult<BudTack.Identity.Internal.Domain.User?>(Rows[index]);
    }

    public Task<bool> DeleteNeverActivatedAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.RemoveAll(row => row.Id == id && row.PasswordHash is null) == 1);

    public Task<BudTack.Identity.Internal.Domain.User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Find(row => row.Id == id));

    public Task<BudTack.Identity.Internal.Domain.User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Find(row => row.Email == email));

    public Task<(IReadOnlyList<BudTack.Identity.Internal.Domain.User> Rows, long Total)> ListAsync(
        int limit, int offset, CancellationToken cancellationToken)
    {
        IReadOnlyList<BudTack.Identity.Internal.Domain.User> page =
            [.. Rows.OrderBy(row => row.Email, StringComparer.Ordinal).Skip(offset).Take(limit)];
        return Task.FromResult((page, (long)Rows.Count));
    }
}
