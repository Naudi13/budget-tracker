using BCrypt.Net;
using BC = BCrypt.Net.BCrypt;

namespace BudTack.Identity.Internal.Application;

/// <summary>
/// BCrypt at cost 12 over a SHA-384 digest of the password (NFR-06, ADR-008):
/// the digest keeps every character significant, where plain BCrypt ignores
/// whatever comes after 72 bytes. The cost is part of the stored value, so it
/// can rise later without breaking the hashes already stored.
/// </summary>
internal sealed class PasswordHasher
{
    public const int DefaultWorkFactor = 12;

    private readonly int _workFactor;

    public PasswordHasher()
        : this(DefaultWorkFactor)
    {
    }

    /// <summary>Tests use a lower cost; production always the default.</summary>
    internal PasswordHasher(int workFactor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workFactor, 4);
        _workFactor = workFactor;
    }

    public string Hash(string password) => BC.EnhancedHashPassword(password, _workFactor);

    /// <summary>False for a wrong password, and for a stored value that is not a BCrypt hash.</summary>
    public static bool Verify(string password, string stored)
    {
        try
        {
            return BC.EnhancedVerify(password, stored);
        }
        catch (Exception exception) when (exception is SaltParseException or BcryptAuthenticationException or ArgumentException or IndexOutOfRangeException)
        {
            return false;
        }
    }
}
