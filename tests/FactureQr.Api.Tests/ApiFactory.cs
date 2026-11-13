using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FactureQr.Api.Tests;

/// <summary>
/// Runs the API in memory against its own database, with a synthetic creditor
/// from Annex A of the Implementation Guidelines and a clock fixed at noon in
/// Zurich on <see cref="Today"/>.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The QR-IBAN of the Annex A examples.</summary>
    public const string QrIban = "CH44 3199 9123 0008 8901 2";

    /// <summary>The plain IBAN of the Annex A examples.</summary>
    public const string PlainIban = "CH58 0079 1123 0008 8901 2";

    private readonly string connectionString;
    private readonly string iban;
    private readonly string town;

    public ApiFactory(string connectionString, string iban = QrIban, string town = "Seldwyla")
    {
        this.connectionString = connectionString;
        this.iban = iban;
        this.town = town;
    }

    /// <summary>The date the API sees as today.</summary>
    public static DateOnly Today { get; } = new(2026, 11, 12);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:FactureQr", connectionString);
        builder.UseSetting("Invoicing:Creditor:Name", "Max Muster & Söhne");
        builder.UseSetting("Invoicing:Creditor:Street", "Musterstrasse");
        builder.UseSetting("Invoicing:Creditor:BuildingNumber", "123");
        builder.UseSetting("Invoicing:Creditor:PostalCode", "8000");
        builder.UseSetting("Invoicing:Creditor:Town", town);
        builder.UseSetting("Invoicing:Creditor:Country", "CH");
        builder.UseSetting("Invoicing:Creditor:Iban", iban);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(Today.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero)));
        });
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
