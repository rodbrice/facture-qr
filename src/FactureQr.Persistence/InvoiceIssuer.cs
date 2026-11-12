using FactureQr.Core.Invoicing;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Persistence;

/// <summary>
/// Issues a stored draft: in one transaction, allocates the next number of
/// the issue year, applies <see cref="Invoice.Issue"/> and saves. Either all
/// of it is committed or nothing is, so a failed issue never consumes a number.
/// </summary>
public sealed class InvoiceIssuer
{
    private readonly FactureQrDbContext db;

    /// <summary>Creates an issuer that works with the given context.</summary>
    public InvoiceIssuer(FactureQrDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        this.db = db;
    }

    /// <summary>Issues the draft and returns its number.</summary>
    /// <param name="invoiceId">The draft to issue.</param>
    /// <param name="issueDate">The issue date; its year selects the number sequence.</param>
    /// <param name="dueDate">The date by which payment is expected.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <exception cref="KeyNotFoundException">No invoice has this identifier.</exception>
    /// <exception cref="InvoiceRuleException">The invoice cannot be issued (not a draft, no lines, total too small, wrong dates).</exception>
    /// <exception cref="DbUpdateConcurrencyException">
    /// Another operation changed or issued the invoice after it was read. Nothing
    /// was saved; the context should be discarded and the operation retried with fresh data.
    /// </exception>
    public async Task<InvoiceNumber> IssueAsync(Guid invoiceId, DateOnly issueDate, DateOnly dueDate, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        Invoice invoice = await db.Invoices
            .Include(i => i.Lines)
            .SingleOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException($"No invoice with the identifier {invoiceId}.");

        InvoiceNumber number = await InvoiceNumberAllocator.AllocateAsync(db, issueDate.Year, cancellationToken);
        invoice.Issue(number, issueDate, dueDate);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return number;
    }
}
