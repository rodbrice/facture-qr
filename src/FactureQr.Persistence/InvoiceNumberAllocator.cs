using FactureQr.Core.Invoicing;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Persistence;

/// <summary>
/// Hands out the next invoice number of a year. One SQL statement creates the
/// year's counter at 1 or increments it, and returns the new value:
/// <code>
/// INSERT INTO invoice_number_counters (year, last_sequence) VALUES (@year, 1)
/// ON CONFLICT (year) DO UPDATE SET last_sequence = invoice_number_counters.last_sequence + 1
/// RETURNING last_sequence
/// </code>
/// The statement locks the year's row until the surrounding transaction ends,
/// so two transactions that issue invoices in the same year take turns and
/// never receive the same number. If the transaction is rolled back, the
/// increment is rolled back with it and the number is not lost.
/// </summary>
internal static class InvoiceNumberAllocator
{
    /// <summary>Allocates the next number of <paramref name="year"/>; must run inside a transaction.</summary>
    public static async Task<InvoiceNumber> AllocateAsync(FactureQrDbContext db, int year, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("An invoice number is allocated only inside the transaction that issues the invoice.");
        }

        List<int> sequence = await db.Database
            .SqlQuery<int>($"""
                INSERT INTO invoice_number_counters (year, last_sequence) VALUES ({year}, 1)
                ON CONFLICT (year) DO UPDATE SET last_sequence = invoice_number_counters.last_sequence + 1
                RETURNING last_sequence AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return new InvoiceNumber(year, sequence.Single());
    }
}
