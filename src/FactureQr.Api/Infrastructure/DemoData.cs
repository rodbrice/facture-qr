using FactureQr.Core.Billing;
using FactureQr.Core.Invoicing;
using FactureQr.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Api.Infrastructure;

/// <summary>
/// The synthetic data of the docker compose demo (ADR-0006): five customers and
/// five invoices that show every state of an invoice (a draft, an issued
/// invoice, an overdue one and two paid ones). The names and addresses are
/// fictitious and come from the examples of the QR-bill standard, like the
/// samples. The dates are counted from today, so the overdue invoice is overdue
/// whenever the demo starts. Everything goes through the domain and
/// <see cref="InvoiceIssuer"/>, so the data follows the same rules as data
/// entered through the API.
/// </summary>
public static class DemoData
{
    /// <summary>The number of customers the seed creates.</summary>
    public const int CustomerCount = 5;

    /// <summary>The number of invoices the seed creates.</summary>
    public const int InvoiceCount = 5;

    /// <summary>
    /// Fills an empty database; does nothing when it already holds a customer or
    /// an invoice, so data entered in the demo is never touched.
    /// </summary>
    /// <param name="db">The database to fill.</param>
    /// <param name="issuer">Issues the invoices, as the API does.</param>
    /// <param name="today">Today's date in Switzerland; the invoice dates are counted from it.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>True when the data was added, false when the database was not empty.</returns>
    public static async Task<bool> SeedAsync(FactureQrDbContext db, InvoiceIssuer issuer, DateOnly today, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(issuer);

        if (await db.Customers.AnyAsync(cancellationToken) || await db.Invoices.AnyAsync(cancellationToken))
        {
            return false;
        }

        var schneider = new Customer(new StructuredAddress("Robert Schneider AG", "Rue du Lac", "1268", "2501", "Biel", "CH"));
        var simon = new Customer(new StructuredAddress("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "CH"));
        var sarah = new Customer(new StructuredAddress("Sarah Beispiel", "Musterstrasse", "1", "8000", "Seldwyla", "CH"));
        var pia = new Customer(new StructuredAddress("Pia-Maria Rutschmann-Schnyder", "Grosse Marktgasse", "28", "9400", "Rorschach", "CH"));
        var foundation = new Customer(new StructuredAddress("Muster Stiftung", null, null, "3001", "Bern", "CH"));
        db.Customers.AddRange(schneider, simon, sarah, pia, foundation);

        // The list orders invoices by identifier, newest first, and identifiers are UUID version 7:
        // ordered by the millisecond they were made in, with no fixed order inside one millisecond.
        // The drafts are therefore made a few milliseconds apart, in the order of their issue dates.
        async Task<Invoice> DraftAsync(Customer customer, params (string Description, decimal Quantity, decimal UnitPrice)[] lines)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken);
            var invoice = Invoice.CreateDraft(customer);
            foreach ((string description, decimal quantity, decimal unitPrice) in lines)
            {
                invoice.AddLine(description, quantity, unitPrice);
            }

            db.Invoices.Add(invoice);
            return invoice;
        }

        Invoice website = await DraftAsync(schneider, ("Développement du site web", 12m, 120m), ("Hébergement, une année", 1m, 180m));
        Invoice advice = await DraftAsync(simon, ("Conseil informatique", 3.5m, 140m));
        Invoice review = await DraftAsync(sarah, ("Relecture d'un rapport annuel", 1m, 340m));
        Invoice translation = await DraftAsync(pia, ("Traduction allemand-français, par page", 8m, 45m));
        await DraftAsync(foundation, ("Atelier de formation, demi-journée", 1m, 650m), ("Supports de cours", 12m, 15m));
        await db.SaveChangesAsync(cancellationToken);

        // Paid: issued 75 days ago, paid before its due date.
        await issuer.IssueAsync(website.Id, today.AddDays(-75), today.AddDays(-45), cancellationToken);
        website.MarkPaid(today.AddDays(-50));

        // Overdue: issued 40 days ago, due 10 days ago, not paid.
        await issuer.IssueAsync(advice.Id, today.AddDays(-40), today.AddDays(-10), cancellationToken);

        // Paid: issued 20 days ago, paid 6 days ago.
        await issuer.IssueAsync(review.Id, today.AddDays(-20), today.AddDays(10), cancellationToken);
        review.MarkPaid(today.AddDays(-6));

        // Issued 3 days ago, due in 27 days.
        await issuer.IssueAsync(translation.Id, today.AddDays(-3), today.AddDays(27), cancellationToken);

        // The workshop stays a draft.
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
