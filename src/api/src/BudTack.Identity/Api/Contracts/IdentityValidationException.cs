namespace BudTack.Identity.Api.Contracts;

/// <summary>The request is malformed: a field is missing or invalid (400 VALIDATION_ERROR).</summary>
public sealed class IdentityValidationException : Exception
{
    public IdentityValidationException(IReadOnlyList<FieldError> errors)
        : base("The request is malformed.")
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors;
    }

    public IReadOnlyList<FieldError> Errors { get; }
}
