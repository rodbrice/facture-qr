using FactureQr.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FactureQr.Api.Infrastructure;

/// <summary>Reports the API as healthy only when the database answers.</summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly FactureQrDbContext db;

    /// <summary>Creates the check with the request's database context.</summary>
    public DatabaseHealthCheck(FactureQrDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        this.db = db;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database does not answer.");
}
