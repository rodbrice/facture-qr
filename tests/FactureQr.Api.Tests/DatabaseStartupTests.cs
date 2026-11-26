using System.Net;
using FactureQr.Api.Customers;
using FactureQr.Api.Infrastructure;
using FactureQr.Api.Invoices;
using FactureQr.Core.Invoicing;

namespace FactureQr.Api.Tests;

/// <summary>
/// The migrations and the synthetic data applied at start for the docker
/// compose demo (DatabaseStartup, DemoData, ADR-0006). Today is
/// <see cref="ApiFactory.Today"/>, 12 November 2026.
/// </summary>
[Collection(PostgreSqlGroup.Name)]
public class DatabaseStartupTests(PostgreSqlFixture postgres)
{
    private static readonly Dictionary<string, string> DemoSettings = new()
    {
        ["Database:ApplyMigrations"] = "true",
        ["Demo:SeedSyntheticData"] = "true",
    };

    [Fact]
    public async Task With_both_settings_a_database_that_does_not_exist_is_created_migrated_and_filled_with_invoices_in_every_state()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), settings: DemoSettings);
        HttpClient client = api.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        List<CustomerResponse> customers = await (await client.GetAsync("/customers")).ReadAsync<List<CustomerResponse>>();
        Assert.Equal(DemoData.CustomerCount, customers.Count);

        List<InvoiceSummary> invoices = await (await client.GetAsync("/invoices")).ReadAsync<List<InvoiceSummary>>();
        Assert.Equal(DemoData.InvoiceCount, invoices.Count);

        // Newest first: the draft, then the issued invoices from the latest issue date to the earliest.
        List<(string, InvoiceStatus, bool, string?, decimal, DateOnly?, DateOnly?, DateOnly?)> expected =
            [
                ("Muster Stiftung", InvoiceStatus.Draft, false, null, 830.00m, null, null, null),
                ("Pia-Maria Rutschmann-Schnyder", InvoiceStatus.Issued, false, "2026-0004", 360.00m, new DateOnly(2026, 11, 9), new DateOnly(2026, 12, 9), null),
                ("Sarah Beispiel", InvoiceStatus.Paid, false, "2026-0003", 340.00m, new DateOnly(2026, 10, 23), new DateOnly(2026, 11, 22), new DateOnly(2026, 11, 6)),
                ("Simon Muster", InvoiceStatus.Issued, true, "2026-0002", 490.00m, new DateOnly(2026, 10, 3), new DateOnly(2026, 11, 2), null),
                ("Robert Schneider AG", InvoiceStatus.Paid, false, "2026-0001", 1620.00m, new DateOnly(2026, 8, 29), new DateOnly(2026, 9, 28), new DateOnly(2026, 9, 23)),
            ];
        Assert.Equal(expected, invoices.Select(i => (i.CustomerName, i.Status, i.Overdue, i.Number, i.Total, i.IssueDate, i.DueDate, i.PaidOn)));

        // Every issued or paid invoice has its PDF.
        foreach (InvoiceSummary invoice in invoices.Where(i => i.Status != InvoiceStatus.Draft))
        {
            HttpResponseMessage pdf = await client.GetAsync($"/invoices/{invoice.Id}/pdf");
            Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
            Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task Restarting_on_a_filled_database_adds_nothing()
    {
        string database = postgres.NewDatabase();
        await using (var first = new ApiFactory(database, settings: DemoSettings))
        {
            first.CreateClient();
        }

        await using var second = new ApiFactory(database, settings: DemoSettings);
        HttpClient client = second.CreateClient();

        Assert.Equal(DemoData.CustomerCount, (await (await client.GetAsync("/customers")).ReadAsync<List<CustomerResponse>>()).Count);
        Assert.Equal(DemoData.InvoiceCount, (await (await client.GetAsync("/invoices")).ReadAsync<List<InvoiceSummary>>()).Count);
    }

    [Fact]
    public async Task A_database_that_already_holds_a_customer_gets_no_synthetic_data()
    {
        string database = await postgres.CreateMigratedDatabaseAsync();
        await using (var plain = new ApiFactory(database))
        {
            await plain.CreateClient().CreateCustomerAsync();
        }

        await using var demo = new ApiFactory(database, settings: DemoSettings);
        HttpClient client = demo.CreateClient();

        CustomerResponse customer = Assert.Single(await (await client.GetAsync("/customers")).ReadAsync<List<CustomerResponse>>());
        Assert.Equal("Simon Muster", customer.Address.Name);
        Assert.Empty(await (await client.GetAsync("/invoices")).ReadAsync<List<InvoiceSummary>>());
    }

    [Fact]
    public async Task Without_the_settings_the_API_neither_migrates_nor_seeds()
    {
        await using (var missing = new ApiFactory(postgres.NewDatabase()))
        {
            // The database was not created: the health check cannot connect.
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await missing.CreateClient().GetAsync("/health")).StatusCode);
        }

        await using var empty = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        Assert.Empty(await (await empty.CreateClient().GetAsync("/customers")).ReadAsync<List<CustomerResponse>>());
    }
}
