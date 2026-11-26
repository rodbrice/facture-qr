using FactureQr.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Api.Infrastructure;

/// <summary>
/// Optional work on the database when the API starts, before it answers
/// requests, used by the docker compose demo (ADR-0006). Both steps are off
/// unless the configuration turns them on:
/// <list type="bullet">
/// <item><c>Database:ApplyMigrations</c> applies the migrations the database does not have yet;</item>
/// <item><c>Demo:SeedSyntheticData</c> fills an empty database with <see cref="DemoData"/>.</item>
/// </list>
/// If a step fails, the API does not start.
/// </summary>
public sealed class DatabaseStartup : IHostedService
{
    private readonly IServiceScopeFactory scopes;
    private readonly SwissCalendar calendar;
    private readonly ILogger<DatabaseStartup> logger;
    private readonly bool applyMigrations;
    private readonly bool seedSyntheticData;

    /// <summary>Reads the two settings.</summary>
    public DatabaseStartup(IServiceScopeFactory scopes, IConfiguration configuration, SwissCalendar calendar, ILogger<DatabaseStartup> logger)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(calendar);
        ArgumentNullException.ThrowIfNull(logger);

        this.scopes = scopes;
        this.calendar = calendar;
        this.logger = logger;
        applyMigrations = configuration.GetValue<bool>("Database:ApplyMigrations");
        seedSyntheticData = configuration.GetValue<bool>("Demo:SeedSyntheticData");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!applyMigrations && !seedSyntheticData)
        {
            return;
        }

        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        FactureQrDbContext db = scope.ServiceProvider.GetRequiredService<FactureQrDbContext>();

        if (applyMigrations)
        {
            List<string> pending = [.. await db.Database.GetPendingMigrationsAsync(cancellationToken)];
            await db.Database.MigrateAsync(cancellationToken);
            ApiLog.MigrationsApplied(logger, pending.Count);
        }

        if (seedSyntheticData)
        {
            InvoiceIssuer issuer = scope.ServiceProvider.GetRequiredService<InvoiceIssuer>();
            if (await DemoData.SeedAsync(db, issuer, calendar.Today, cancellationToken))
            {
                ApiLog.SyntheticDataSeeded(logger, DemoData.CustomerCount, DemoData.InvoiceCount);
            }
            else
            {
                ApiLog.SyntheticDataSkipped(logger);
            }
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
