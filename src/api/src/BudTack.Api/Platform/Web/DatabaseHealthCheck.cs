using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// The one required dependency <c>GET /health</c> reports on: the database,
/// reached with the API's own role (13-operations/observability.md).
/// </summary>
internal sealed class DatabaseHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return HealthCheckResult.Unhealthy("The database is unreachable.", error);
        }
    }
}
