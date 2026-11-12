using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FactureQr.Persistence.Tests;

[Collection(PostgreSqlGroup.Name)]
public class MigrationTests(PostgreSqlFixture postgres)
{
    [Fact]
    public async Task The_migrations_apply_to_an_empty_database_and_match_the_model()
    {
        string connectionString = postgres.NewDatabaseConnectionString();
        await using var db = new FactureQrDbContext(FactureQrDbContext.CreateOptions(connectionString));

        await db.Database.MigrateAsync();

        Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "The model has changes that no migration contains; run `dotnet ef migrations add`.");

        List<string> tables = await db.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name")
            .ToListAsync();
        Assert.Equal(["__EFMigrationsHistory", "customers", "invoice_lines", "invoice_number_counters", "invoices"], tables);
    }

    [Fact]
    public async Task Applying_the_migrations_twice_changes_nothing()
    {
        string connectionString = await postgres.CreateMigratedDatabaseAsync();
        await using var db = new FactureQrDbContext(FactureQrDbContext.CreateOptions(connectionString));

        await db.Database.MigrateAsync();

        Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task The_database_refuses_a_second_invoice_with_the_same_number()
    {
        string connectionString = await postgres.CreateMigratedDatabaseAsync();
        await using var db = new FactureQrDbContext(FactureQrDbContext.CreateOptions(connectionString));
        Guid customerId = await InsertCustomerAsync(db);
        await InsertIssuedInvoiceAsync(db, customerId, 2026, 1);

        var e = await Assert.ThrowsAsync<PostgresException>(() => InsertIssuedInvoiceAsync(db, customerId, 2026, 1));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, e.SqlState);
        Assert.Equal("ux_invoices_number", e.ConstraintName);
    }

    [Fact]
    public async Task The_database_refuses_an_issued_invoice_whose_number_year_differs_from_the_issue_date()
    {
        string connectionString = await postgres.CreateMigratedDatabaseAsync();
        await using var db = new FactureQrDbContext(FactureQrDbContext.CreateOptions(connectionString));
        Guid customerId = await InsertCustomerAsync(db);

        var e = await Assert.ThrowsAsync<PostgresException>(() => InsertIssuedInvoiceAsync(db, customerId, 2025, 1));

        Assert.Equal(PostgresErrorCodes.CheckViolation, e.SqlState);
        Assert.Equal("ck_invoices_issued_fields", e.ConstraintName);
    }

    [Theory]
    [InlineData("0.00")]
    [InlineData("1000000000.00")]
    public async Task The_database_refuses_an_issued_total_outside_the_QR_bill_amount_range(string total)
    {
        string connectionString = await postgres.CreateMigratedDatabaseAsync();
        await using var db = new FactureQrDbContext(FactureQrDbContext.CreateOptions(connectionString));
        Guid customerId = await InsertCustomerAsync(db);

        var e = await Assert.ThrowsAnyAsync<PostgresException>(() => InsertIssuedInvoiceAsync(db, customerId, 2026, 1, decimal.Parse(total, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.True(
            e.SqlState is PostgresErrorCodes.CheckViolation or PostgresErrorCodes.NumericValueOutOfRange,
            $"Unexpected error {e.SqlState}: {e.MessageText}");
    }

    private static async Task<Guid> InsertCustomerAsync(FactureQrDbContext db)
    {
        var id = Guid.NewGuid();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO customers (id, address_name, address_street, address_building_number, address_postal_code, address_town, address_country)
            VALUES ({id}, 'Simon Muster', 'Musterstrasse', '1', '8000', 'Seldwyla', 'CH')
            """);
        return id;
    }

    private static Task<int> InsertIssuedInvoiceAsync(FactureQrDbContext db, Guid customerId, int year, int sequence, decimal total = 100m)
    {
        var issueDate = new DateOnly(2026, 3, 2);
        return db.Database.ExecuteSqlAsync($"""
            INSERT INTO invoices (id, customer_id, status, total, number_year, number_sequence, issue_date, due_date, revision,
                                  bill_to_name, bill_to_street, bill_to_building_number, bill_to_postal_code, bill_to_town, bill_to_country)
            VALUES ({Guid.NewGuid()}, {customerId}, 'Issued', {total}, {year}, {sequence}, {issueDate}, {issueDate.AddDays(30)}, 1,
                    'Simon Muster', 'Musterstrasse', '1', '8000', 'Seldwyla', 'CH')
            """);
    }
}
