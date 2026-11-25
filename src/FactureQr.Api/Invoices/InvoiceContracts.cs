using FactureQr.Api.Customers;
using FactureQr.Core.Invoicing;

namespace FactureQr.Api.Invoices;

/// <summary>A line to add to a draft.</summary>
/// <param name="Description">What is invoiced, at most 200 characters, on one line. Typographic apostrophes, quotes, dashes and the ellipsis are replaced by their plain equivalents; a character the PDF cannot print is refused.</param>
/// <param name="Quantity">Greater than 0, at most three decimals.</param>
/// <param name="UnitPrice">Price of one unit in CHF, 0 or more, at most two decimals.</param>
public sealed record LineRequest(string Description, decimal Quantity, decimal UnitPrice);

/// <summary>A new draft for a customer, optionally with its first lines.</summary>
/// <param name="CustomerId">The customer; the draft copies the customer's current address.</param>
/// <param name="Lines">The lines to add, in order; may be omitted.</param>
public sealed record CreateInvoiceRequest(Guid CustomerId, IReadOnlyList<LineRequest>? Lines = null);

/// <summary>The dates of an issue. Both may be omitted.</summary>
/// <param name="IssueDate">The issue date; today in Switzerland when omitted.</param>
/// <param name="DueDate">The due date; the issue date plus 30 days when omitted.</param>
public sealed record IssueRequest(DateOnly? IssueDate = null, DateOnly? DueDate = null);

/// <summary>The date a payment was received.</summary>
/// <param name="PaidOn">The payment date; today in Switzerland when omitted. Not before the issue date, not in the future.</param>
public sealed record MarkPaidRequest(DateOnly? PaidOn = null);

/// <summary>A line of an invoice.</summary>
/// <param name="Id">The identifier, used to remove the line from a draft.</param>
/// <param name="Position">Position on the invoice, starting at 1.</param>
/// <param name="Description">What is invoiced.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="UnitPrice">The price of one unit in CHF.</param>
/// <param name="Amount">Quantity × unit price, rounded to the centime.</param>
public sealed record LineResponse(Guid Id, int Position, string Description, decimal Quantity, decimal UnitPrice, decimal Amount);

/// <summary>An invoice with its lines.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Number">The number (<c>2026-0001</c>), or null for a draft.</param>
/// <param name="Status">Draft, Issued or Paid.</param>
/// <param name="Overdue">True when issued, not paid, and past its due date (Swiss date).</param>
/// <param name="CustomerId">The customer.</param>
/// <param name="BillTo">The address copied onto the invoice.</param>
/// <param name="Lines">The lines, in order.</param>
/// <param name="Total">The sum of the line amounts, in CHF.</param>
/// <param name="IssueDate">The issue date, or null for a draft.</param>
/// <param name="DueDate">The due date, or null for a draft.</param>
/// <param name="PaidOn">The payment date, or null when not paid.</param>
public sealed record InvoiceResponse(
    Guid Id,
    string? Number,
    InvoiceStatus Status,
    bool Overdue,
    Guid CustomerId,
    AddressDto BillTo,
    IReadOnlyList<LineResponse> Lines,
    decimal Total,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    DateOnly? PaidOn)
{
    /// <summary>Copies a stored invoice; <paramref name="today"/> decides whether it is overdue.</summary>
    public static InvoiceResponse From(Invoice invoice, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new(
            invoice.Id,
            invoice.Number?.ToString(),
            invoice.Status,
            invoice.IsOverdue(today),
            invoice.CustomerId,
            AddressDto.From(invoice.BillTo),
            [.. invoice.Lines.Select(l => new LineResponse(l.Id, l.Position, l.Description, l.Quantity, l.UnitPrice, l.Amount))],
            invoice.Total,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.PaidOn);
    }
}

/// <summary>An invoice in a list, without its lines.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Number">The number, or null for a draft.</param>
/// <param name="Status">Draft, Issued or Paid.</param>
/// <param name="Overdue">True when issued, not paid, and past its due date.</param>
/// <param name="CustomerId">The customer.</param>
/// <param name="CustomerName">The name copied onto the invoice.</param>
/// <param name="Total">The total in CHF.</param>
/// <param name="IssueDate">The issue date, or null for a draft.</param>
/// <param name="DueDate">The due date, or null for a draft.</param>
/// <param name="PaidOn">The payment date, or null when not paid.</param>
public sealed record InvoiceSummary(
    Guid Id,
    string? Number,
    InvoiceStatus Status,
    bool Overdue,
    Guid CustomerId,
    string CustomerName,
    decimal Total,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    DateOnly? PaidOn)
{
    /// <summary>Copies a stored invoice; <paramref name="today"/> decides whether it is overdue.</summary>
    public static InvoiceSummary From(Invoice invoice, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new(
            invoice.Id,
            invoice.Number?.ToString(),
            invoice.Status,
            invoice.IsOverdue(today),
            invoice.CustomerId,
            invoice.BillTo.Name,
            invoice.Total,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.PaidOn);
    }
}
