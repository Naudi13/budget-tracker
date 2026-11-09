using BudTack.Transactions.Internal.Application;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>Who is who, without identity.</summary>
internal sealed class InMemoryAccountNames : IAccountNames
{
    public Dictionary<Guid, string> Names { get; } = [];

    public Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(
            Names.Where(n => userIds.Contains(n.Key)).ToDictionary(n => n.Key, n => n.Value));
}
