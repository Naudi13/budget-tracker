namespace BudTack.Transactions.Api.Contracts;

/// <summary>The role may not do this (403 FORBIDDEN).</summary>
public sealed class TransactionsForbiddenException()
    : Exception("Your role may not do this.");
