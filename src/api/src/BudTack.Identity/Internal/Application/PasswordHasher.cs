using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BudTack.Identity.Internal.Application;

/// <summary>
/// Salted PBKDF2-HMAC-SHA256 (NFR-06, security-policy.md). The stored value is
/// <c>pbkdf2-sha256$&lt;iterations&gt;$&lt;salt&gt;$&lt;hash&gt;</c>, base64, so the cost can
/// rise later without breaking the hashes already stored.
/// </summary>
internal sealed class PasswordHasher
{
    public const string Prefix = "pbkdf2-sha256";

    /// <summary>OWASP's 2023 figure for PBKDF2-HMAC-SHA256.</summary>
    public const int DefaultIterations = 600_000;

    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    private readonly int _iterations;

    public PasswordHasher()
        : this(DefaultIterations)
    {
    }

    /// <summary>Tests use fewer iterations; production always the default.</summary>
    internal PasswordHasher(int iterations)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        _iterations = iterations;
    }

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(password, salt, _iterations);
        return string.Join(
            '$',
            Prefix,
            _iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4
            || parts[0] != Prefix
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
            || iterations < 1)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            return CryptographicOperations.FixedTimeEquals(Derive(password, salt, iterations), expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, HashBytes);
}
