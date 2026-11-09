namespace BudTack.Transactions.Api.Contracts;

/// <summary>A recorded transaction (transactions.yaml <c>Transaction</c>), with its lines.</summary>
public sealed record Transaction(Guid Id, DateOnly TransactionDate, string Description, IReadOnlyList<TransactionDetail> Details);
