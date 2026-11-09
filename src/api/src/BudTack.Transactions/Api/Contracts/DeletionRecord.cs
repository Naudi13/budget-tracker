namespace BudTack.Transactions.Api.Contracts;

/// <summary>Who deleted a transaction, when and why (transactions.yaml <c>DeletionRecord</c>).</summary>
public sealed record DeletionRecord(DateTimeOffset DeletedAt, string DeletedBy, string Reason);
