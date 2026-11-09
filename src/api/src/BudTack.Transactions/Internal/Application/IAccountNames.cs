namespace BudTack.Transactions.Internal.Application;

/// <summary>The names behind the user ids an ADMIN sees on transactions.</summary>
internal interface IAccountNames
{
    Task<IReadOnlyDictionary<Guid, string>> NamesOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
