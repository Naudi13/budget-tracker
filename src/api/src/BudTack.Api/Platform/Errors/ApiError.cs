namespace BudTack.Api.Platform.Errors;

/// <summary>
/// The one error body of the API, per 07-api/rest-conventions.md and
/// <c>common.yaml#/components/schemas/Error</c>. <see cref="Details"/> is only
/// present on 400 and 422.
/// </summary>
internal sealed record ApiError(string Error, string Message, IReadOnlyList<ApiErrorDetail>? Details = null);

/// <summary>One field-level problem inside a 400 or 422 response.</summary>
internal sealed record ApiErrorDetail(string Field, string Message);

/// <summary>The stable machine codes; never renamed once published.</summary>
internal static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string BusinessRuleViolated = "BUSINESS_RULE_VIOLATED";
    public const string Conflict = "CONFLICT";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InternalError = "INTERNAL_ERROR";
}
