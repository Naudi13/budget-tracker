namespace BudTack.Transactions.Api.Contracts;

/// <summary>Who reads or deletes transactions: an ADMIN reaches them all, anyone else their own (ADR-009).</summary>
public sealed record Viewer(Guid UserId, bool IsAdmin);
