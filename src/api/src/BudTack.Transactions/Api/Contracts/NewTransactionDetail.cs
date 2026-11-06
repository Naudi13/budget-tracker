namespace BudTack.Transactions.Api.Contracts;

/// <summary>One line of a new transaction. It carries no type: the server derives it (BR-01).</summary>
public sealed record NewTransactionDetail(Guid? SubcategoryId, decimal? Amount);
