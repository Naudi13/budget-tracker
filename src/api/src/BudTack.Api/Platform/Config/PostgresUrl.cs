using Npgsql;

namespace BudTack.Api.Platform.Config;

/// <summary>
/// Turns a <c>postgresql://user:password@host:port/database?sslmode=...</c> URI —
/// the shape of <c>DATABASE_URL</c> — into the key-value connection
/// string Npgsql expects.
/// </summary>
internal static class PostgresUrl
{
    public static string ToConnectionString(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgresql" && uri.Scheme != "postgres"))
        {
            throw new FormatException(
                "The database URL must look like postgresql://user:password@host:5432/database");
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (userInfo[0].Length == 0 || database.Length == 0)
        {
            throw new FormatException("The database URL must name a user and a database.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            Database = database,
            // No Kerberos anywhere in this stack; without this Npgsql probes for
            // libgssapi_krb5 on every connection and logs an error in the image.
            GssEncryptionMode = GssEncryptionMode.Disable,
        };

        var sslMode = QueryValue(uri.Query, "sslmode");
        if (sslMode is not null)
        {
            builder.SslMode = Enum.Parse<SslMode>(sslMode, ignoreCase: true);
        }

        return builder.ConnectionString;
    }

    private static string? QueryValue(string query, string key)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (string.Equals(Uri.UnescapeDataString(parts[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            }
        }

        return null;
    }
}
