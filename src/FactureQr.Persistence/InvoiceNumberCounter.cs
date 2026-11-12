namespace FactureQr.Persistence;

/// <summary>
/// The last invoice sequence used in a year. One row per year; the row is
/// created by the first invoice of the year and incremented by each
/// following one, inside the transaction that issues the invoice
/// (see <see cref="InvoiceNumberAllocator"/> and ADR-0003).
/// </summary>
internal sealed class InvoiceNumberCounter
{
    public int Year { get; set; }

    public int LastSequence { get; set; }
}
