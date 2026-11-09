using BudTack.Identity.Api.Contracts;

namespace BudTack.Identity.Internal.Domain;

/// <summary>
/// What a new account, a link confirmation and a sign-in must carry (identity.yaml,
/// screens users.md, activate-account.md and sign-in.md). Every problem is reported at once, with the message the
/// screen shows, so the user fixes the whole form in one pass.
/// </summary>
internal static class AccountRules
{
    public const string InvalidLink = "The link is not valid or has expired.";
    public const int NameMaxLength = 100;
    public const int PasswordMinLength = 8;

    /// <summary>Bounds the work of hashing; far above any real password.</summary>
    public const int PasswordMaxLength = 128;

    public static (string Name, string Email, string Role) Validate(NewUser request)
    {
        var errors = new List<FieldError>();

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > NameMaxLength)
        {
            errors.Add(new FieldError("name", "Enter the name."));
        }

        var email = EmailAddress.Normalize(request.Email ?? string.Empty);
        if (!EmailAddress.IsValid(email))
        {
            errors.Add(new FieldError("email", "Enter an email, for example owner@example.com."));
        }

        var role = request.Role;
        if (role is not (Roles.User or Roles.Admin))
        {
            errors.Add(new FieldError("role", "Choose USER or ADMIN."));
        }

        return errors.Count == 0 ? (name, email, role!) : throw new IdentityValidationException(errors);
    }

    /// <summary>A token that cannot be valid answers like any other invalid one.</summary>
    public static (string Token, string Password) Validate(LinkConfirmation request)
    {
        var errors = new List<FieldError>();

        var token = request.Token?.Trim() ?? string.Empty;
        if (token.Length == 0)
        {
            errors.Add(new FieldError("token", InvalidLink));
        }

        var password = request.Password ?? string.Empty;
        if (password.Length < PasswordMinLength)
        {
            errors.Add(new FieldError("password", "Use at least 8 characters."));
        }
        else if (password.Length > PasswordMaxLength)
        {
            errors.Add(new FieldError("password", "Use at most 128 characters."));
        }

        return errors.Count == 0 ? (token, password) : throw new IdentityValidationException(errors);
    }

    public static (string Email, string Password) Validate(Credentials request)
    {
        var errors = new List<FieldError>();

        var email = EmailAddress.Normalize(request.Email ?? string.Empty);
        if (!EmailAddress.IsValid(email))
        {
            errors.Add(new FieldError("email", "Enter the email you signed up with, for example owner@example.com."));
        }

        var password = request.Password ?? string.Empty;
        if (password.Length == 0)
        {
            errors.Add(new FieldError("password", "Enter your password."));
        }

        return errors.Count == 0 ? (email, password) : throw new IdentityValidationException(errors);
    }
}
