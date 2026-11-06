namespace BudTack.Transactions.Api.Contracts;

/// <summary>
/// The request is well formed but breaks a business rule (422
/// BUSINESS_RULE_VIOLATED): income and expense lines in one transaction (BR-02),
/// or a line against an inactive subcategory or category (BR-05).
/// </summary>
public sealed class TransactionsBusinessRuleException : Exception
{
    public TransactionsBusinessRuleException(string message, IReadOnlyList<FieldError> errors)
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    public IReadOnlyList<FieldError> Errors { get; }
}
