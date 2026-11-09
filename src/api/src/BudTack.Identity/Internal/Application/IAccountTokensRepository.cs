using BudTack.Identity.Internal.Domain;

namespace BudTack.Identity.Internal.Application;

/// <summary>identity.account_tokens, as the links need it (ADR-010).</summary>
internal interface IAccountTokensRepository
{
    /// <summary>Stores a link and cancels every earlier unused link of its account.</summary>
    Task ReplaceAsync(AccountToken token, CancellationToken cancellationToken);

    /// <summary>In one step: spends the link and sets the password. False when the link is unknown, expired or used.</summary>
    Task<bool> ConfirmAsync(string tokenHash, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken);
}
