namespace BudTack.Transactions.Api.Contracts;

/// <summary>The request is malformed: a field or query parameter is missing or invalid (400 VALIDATION_ERROR).</summary>
public sealed class TransactionsValidationException : Exception
{
    public TransactionsValidationException(IReadOnlyList<FieldError> errors)
        : base("The request is malformed.")
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    public IReadOnlyList<FieldError> Errors { get; }
}
