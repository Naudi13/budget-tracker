using System.Text;

namespace BudTack.Identity;

/// <summary>
/// What the identity module needs from the environment. The host reads it
/// from <c>AUTH_JWT_SECRET</c> and <c>AUTH_JWT_EXPIRY_SECONDS</c>; the module
/// never reads configuration itself (05-architecture/module-structure.md).
/// </summary>
public sealed record IdentitySettings
{
    /// <summary>HS256 needs a key of at least 256 bits.</summary>
    public const int MinimumSecretBytes = 32;

    public IdentitySettings(string jwtSecret, TimeSpan tokenLifetime)
    {
        ArgumentNullException.ThrowIfNull(jwtSecret);
        if (Encoding.UTF8.GetByteCount(jwtSecret) < MinimumSecretBytes)
        {
            throw new ArgumentException(
                $"The token secret must be at least {MinimumSecretBytes} bytes long.", nameof(jwtSecret));
        }

        if (tokenLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenLifetime), "The token lifetime must be positive.");
        }

        JwtSecret = jwtSecret;
        TokenLifetime = tokenLifetime;
    }

    public string JwtSecret { get; }

    public TimeSpan TokenLifetime { get; }

    /// <summary>Never print the secret, not even in a debugger or a log of this record.</summary>
    public override string ToString() => $"IdentitySettings {{ TokenLifetime = {TokenLifetime} }}";
}
