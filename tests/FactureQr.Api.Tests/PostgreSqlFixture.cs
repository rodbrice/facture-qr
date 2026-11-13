using FactureQr.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FactureQr.Api.Tests;

/// <summary>
/// Starts one PostgreSQL server in Docker for all the API tests and gives each
/// test its own empty database, created by applying the migrations.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    /// <summary>Creates a new database, applies every migration to it and returns its connection string.</summary>
    public async Task<string> CreateMigratedDatabaseAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = $"facture_qr_{Guid.NewGuid():N}",
        };
        await using var db = new FactureQrDbContext(FactureQrDbContext.CreateOptions(builder.ConnectionString));
        await db.Database.MigrateAsync();
        return builder.ConnectionString;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlGroup : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
