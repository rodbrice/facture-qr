using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FactureQr.Persistence.Tests;

/// <summary>
/// Starts one PostgreSQL server in Docker for all the tests and gives each test
/// its own empty database, created by applying the migrations. Tests therefore
/// never share data and always run against the schema the migrations produce.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    /// <summary>The connection string of a new database that does not exist yet.</summary>
    public string NewDatabaseConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = $"facture_qr_{Guid.NewGuid():N}",
        };
        return builder.ConnectionString;
    }

    /// <summary>Creates a new database, applies every migration to it and returns its connection string.</summary>
    public async Task<string> CreateMigratedDatabaseAsync()
    {
        string connectionString = NewDatabaseConnectionString();
        await using var db = new FactureQrDbContext(FactureQrDbContext.CreateOptions(connectionString));
        await db.Database.MigrateAsync();
        return connectionString;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlGroup : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL";
}
