using System.Globalization;
using BudTack.Identity;

namespace BudTack.Api.Platform.Config;

/// <summary>
/// Reads the identity module's settings from <c>AUTH_JWT_SECRET</c> and
/// <c>AUTH_JWT_EXPIRY_SECONDS</c> (.env.example). A missing or short secret
/// stops the host at startup rather than issuing tokens anyone could forge.
/// </summary>
internal static class IdentityConfiguration
{
    public const int DefaultExpirySeconds = 3600;

    public static IdentitySettings Read(IConfiguration configuration)
    {
        var secret = configuration["AUTH_JWT_SECRET"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "AUTH_JWT_SECRET is not set. Copy .env.example to .env, or set it in the environment.");
        }

        var expiry = configuration["AUTH_JWT_EXPIRY_SECONDS"];
        var seconds = DefaultExpirySeconds;
        if (!string.IsNullOrWhiteSpace(expiry)
            && (!int.TryParse(expiry, NumberStyles.None, CultureInfo.InvariantCulture, out seconds) || seconds < 1))
        {
            throw new InvalidOperationException("AUTH_JWT_EXPIRY_SECONDS must be a whole number of seconds above 0.");
        }

        try
        {
            return new IdentitySettings(secret, TimeSpan.FromSeconds(seconds));
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"AUTH_JWT_SECRET must be at least {IdentitySettings.MinimumSecretBytes} bytes long.", exception);
        }
    }
}
