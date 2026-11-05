using System.Text.RegularExpressions;

namespace BudTack.Identity.Internal.Domain;

/// <summary>
/// An email as identity stores and compares it: trimmed and lowercased, so
/// <c>Owner@Example.com</c> and <c>owner@example.com</c> are one account. The
/// database refuses any other form (<c>ck_users_email_lowercase</c>).
/// </summary>
internal static partial class EmailAddress
{
    /// <summary>The longest address SMTP allows, and the width of identity.users.email.</summary>
    public const int MaxLength = 254;

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    /// <summary>One <c>@</c>, something on both sides, a dot in the domain, no spaces.</summary>
    public static bool IsValid(string normalized) =>
        normalized.Length <= MaxLength && Shape().IsMatch(normalized);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Shape();
}
