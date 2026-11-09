namespace BudTack.Api.Platform.Logging;

/// <summary>
/// The correlation id of the current request: generated once at the edge, or
/// taken from a well-formed <c>X-Correlation-Id</c> request header, and carried
/// as a logging scope through every module the request crosses.
/// </summary>
internal static class CorrelationScope
{
    public const string Key = "correlationId";
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxLength = 64;

    /// <summary>Keeps a caller's id only when it is short and plainly safe to log.</summary>
    public static string Resolve(string? incoming) =>
        IsAcceptable(incoming) ? incoming! : Guid.NewGuid().ToString("N");

    public static bool IsAcceptable(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= MaxLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
