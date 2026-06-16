using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Host.HealthChecks;

/// <summary>
/// Health check that verifies SQL Server database connectivity for all module schemas.
/// Returns Degraded (not Unhealthy) on timeout per edge case spec.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public DatabaseHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("IdentityDb connection string not configured.");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Timeout);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cts.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cts.Token);

            return HealthCheckResult.Healthy("SQL Server is reachable.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("SQL Server health check timed out after 5s.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("SQL Server is unreachable.", ex);
        }
    }
}
