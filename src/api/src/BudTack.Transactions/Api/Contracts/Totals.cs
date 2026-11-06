namespace BudTack.Transactions.Api.Contracts;

/// <summary>
/// The owner's income and expense totals, computed on read from every line of
/// every transaction not deleted (BR-03, BR-04); never stored.
/// </summary>
public sealed record Totals(decimal Income, decimal Expenses);
