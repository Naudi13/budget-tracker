using BudTack.Identity.Internal.Application;
using BudTack.Identity.Internal.Domain;

namespace BudTack.Unit.Tests.Identity;

/// <summary>identity.account_tokens in memory, sharing the users table with <see cref="InMemoryUsers"/>.</summary>
internal sealed class InMemoryAccountTokens(InMemoryUsers users) : IAccountTokensRepository
{
    public List<AccountToken> Rows { get; } = [];

    public HashSet<Guid> Used { get; } = [];

    public Task ReplaceAsync(AccountToken token, CancellationToken cancellationToken)
    {
        foreach (var row in Rows.Where(r => r.UserId == token.UserId))
        {
            Used.Add(row.Id);
        }

        Rows.Add(token);
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string tokenHash, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var token = Rows.Find(r => r.TokenHash == tokenHash && !Used.Contains(r.Id) && r.ExpiresAt > now);
        if (token is null)
        {
            return Task.FromResult(false);
        }

        Used.Add(token.Id);
        var index = users.Rows.FindIndex(u => u.Id == token.UserId);
        users.Rows[index] = users.Rows[index] with { PasswordHash = passwordHash };
        return Task.FromResult(true);
    }
}
