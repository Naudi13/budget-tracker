using Npgsql;

namespace BudTack.Api.Platform.Config;

/// <summary>
/// Registers the one <see cref="NpgsqlDataSource"/> of the application, built
/// once at startup from <c>DATABASE_URL</c> — the API's own role,
/// <c>bud_tack_app</c>, never the migrator's.
/// </summary>
internal static class DatabaseServiceCollectionExtensions
{
    public const string DatabaseUrlKey = "DATABASE_URL";

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var url = configuration[DatabaseUrlKey];
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException(
                $"{DatabaseUrlKey} is not set. Copy .env.example to .env, or set it in the environment.");
        }

        var connectionString = PostgresUrl.ToConnectionString(url);
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        return services;
    }
}
