using Microsoft.EntityFrameworkCore.Design;

namespace FactureQr.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create the context to add migrations or produce SQL
/// scripts. Adding a migration does not connect to a database; commands that
/// do (<c>database update</c>) read the connection string from the
/// <c>FACTUREQR_DATABASE</c> environment variable.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FactureQrDbContext>
{
    public FactureQrDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("FACTUREQR_DATABASE")
            ?? "Host=localhost;Database=facture_qr;Username=postgres";

        return new FactureQrDbContext(FactureQrDbContext.CreateOptions(connectionString));
    }
}
