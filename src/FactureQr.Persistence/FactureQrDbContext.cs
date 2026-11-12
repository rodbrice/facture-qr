using FactureQr.Core.Invoicing;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Persistence;

/// <summary>
/// The database of Facture QR: customers, invoices with their lines, and one
/// counter per year for invoice numbers. The mapping lives in
/// <c>Configurations/</c>; the schema is created and changed only by the
/// migrations in <c>Migrations/</c>.
/// </summary>
public sealed class FactureQrDbContext : DbContext
{
    /// <summary>Creates a context with the given options (provider and connection string).</summary>
    public FactureQrDbContext(DbContextOptions<FactureQrDbContext> options)
        : base(options)
    {
    }

    /// <summary>The customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>The invoices; their lines are loaded with <c>Include(i => i.Lines)</c>.</summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>
    /// Builds the options for PostgreSQL with snake_case names. The same
    /// options are used by the application, the tests and the migration tool.
    /// </summary>
    public static DbContextOptions<FactureQrDbContext> CreateOptions(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new DbContextOptionsBuilder<FactureQrDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FactureQrDbContext).Assembly);
    }
}
