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
        string connectionString = NewDatabase();
        await using var db = new FactureQrDbContext(FactureQrDbContext.CreateOptions(connectionString));
        await db.Database.MigrateAsync();
        return connectionString;
    }

    /// <summary>
    /// Returns the connection string of a database that does not exist yet;
    /// applying the migrations creates it.
    /// </summary>
    public string NewDatabase() =>
        new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = $"facture_qr_{Guid.NewGuid():N}" }.ConnectionString;
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlGroup : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
