using FactureQr.Core.Billing;

namespace FactureQr.Core.Invoicing;

/// <summary>
/// An invoice to one customer, in CHF, with its lines. The class enforces the
/// rules of the invoice model:
/// <list type="bullet">
/// <item>the total is always the sum of the line amounts, and never exceeds the largest amount of a QR-bill;</item>
/// <item>only a draft can change; issuing gives it a number and dates and freezes its content;</item>
/// <item>an invoice can be issued only with at least one line and a total of at least 0.01 CHF, the smallest amount of a QR-bill;</item>
/// <item>an issued invoice can only be marked as paid, and a paid invoice is final.</item>
/// </list>
/// Every change increments <see cref="Revision"/>, which the persistence layer
/// uses to detect concurrent changes (see ADR-0003).
/// </summary>
public sealed class Invoice
{
    /// <summary>Smallest total an invoice can be issued with: the smallest amount of a QR-bill.</summary>
    public const decimal MinIssuedTotal = QrBillRequest.MinAmount;

    /// <summary>Largest total: the largest amount of a QR-bill.</summary>
    public const decimal MaxTotal = QrBillRequest.MaxAmount;

    private readonly List<InvoiceLine> lines = [];

    private Invoice(Customer customer)
    {
        Id = Guid.CreateVersion7();
        CustomerId = customer.Id;
        BillTo = customer.Address;
        Status = InvoiceStatus.Draft;
    }

    // Used by the persistence layer to materialise a stored invoice.
    private Invoice()
    {
        BillTo = null!;
    }

    /// <summary>The identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>The customer the invoice is addressed to.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// The customer's address as it was copied onto the invoice. It is kept
    /// with the invoice so that an issued invoice does not change when the
    /// customer's address changes later.
    /// </summary>
    public StructuredAddress BillTo { get; private set; }

    /// <summary>The stored state.</summary>
    public InvoiceStatus Status { get; private set; }

    /// <summary>The lines, in the order they were added.</summary>
    public IReadOnlyList<InvoiceLine> Lines => [.. lines.OrderBy(l => l.Position)];

    /// <summary>The sum of the line amounts, in CHF.</summary>
    public decimal Total { get; private set; }

    /// <summary>The year part of the number, or null for a draft.</summary>
    public int? NumberYear { get; private set; }

    /// <summary>The sequence part of the number, or null for a draft.</summary>
    public int? NumberSequence { get; private set; }

    /// <summary>The invoice number, or null for a draft.</summary>
    public InvoiceNumber? Number => NumberYear is int year && NumberSequence is int sequence ? new InvoiceNumber(year, sequence) : null;

    /// <summary>The issue date, or null for a draft.</summary>
    public DateOnly? IssueDate { get; private set; }

    /// <summary>The date by which payment is expected, or null for a draft.</summary>
    public DateOnly? DueDate { get; private set; }

    /// <summary>The date the payment was received, or null when not paid.</summary>
    public DateOnly? PaidOn { get; private set; }

    /// <summary>Incremented by every change; used to detect concurrent changes.</summary>
    public int Revision { get; private set; }

    /// <summary>Creates an empty draft for the customer and copies the customer's address.</summary>
    public static Invoice CreateDraft(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        return new Invoice(customer);
    }

    /// <summary>
    /// True when the invoice is issued, not paid, and <paramref name="today"/>
    /// is after the due date. Overdue is derived, never stored.
    /// </summary>
    public bool IsOverdue(DateOnly today) => Status == InvoiceStatus.Issued && today > DueDate;

    /// <summary>Adds a line at the end of the draft and returns it.</summary>
    /// <exception cref="InvoiceRuleException">The invoice is not a draft, a value is invalid, or the new total would exceed the QR-bill maximum.</exception>
    public InvoiceLine AddLine(string description, decimal quantity, decimal unitPrice)
    {
        EnsureDraft("add a line to");

        var line = new InvoiceLine(lines.Count + 1, description, quantity, unitPrice);
        decimal total = Total + line.Amount;
        if (total > MaxTotal)
        {
            throw new InvoiceRuleException($"The total would be {total} CHF, more than the largest amount a QR-bill can carry ({MaxTotal} CHF).");
        }

        lines.Add(line);
        Total = total;
        Revision++;
        return line;
    }

    /// <summary>Removes a line from the draft; the following lines move up one position.</summary>
    /// <exception cref="InvoiceRuleException">The invoice is not a draft or has no such line.</exception>
    public void RemoveLine(Guid lineId)
    {
        EnsureDraft("remove a line from");

        InvoiceLine line = lines.Find(l => l.Id == lineId)
            ?? throw new InvoiceRuleException($"The invoice has no line {lineId}.");

        lines.Remove(line);
        foreach (InvoiceLine next in lines.Where(l => l.Position > line.Position))
        {
            next.Position--;
        }

        Total = lines.Sum(l => l.Amount);
        Revision++;
    }

    /// <summary>
    /// Issues the draft: gives it its number, issue date and due date. From
    /// then on, its customer, address and lines can no longer change.
    /// </summary>
    /// <param name="number">The number allocated for the year of <paramref name="issueDate"/>.</param>
    /// <param name="issueDate">The issue date.</param>
    /// <param name="dueDate">The date by which payment is expected; not before the issue date.</param>
    /// <exception cref="InvoiceRuleException">A rule is violated; nothing is changed.</exception>
    public void Issue(InvoiceNumber number, DateOnly issueDate, DateOnly dueDate)
    {
        ArgumentNullException.ThrowIfNull(number);
        EnsureDraft("issue");

        if (lines.Count == 0)
        {
            throw new InvoiceRuleException("An invoice needs at least one line to be issued.");
        }

        if (Total < MinIssuedTotal)
        {
            throw new InvoiceRuleException($"An invoice is issued with a total of at least {MinIssuedTotal} CHF, the smallest amount of a QR-bill (got {Total}).");
        }

        if (number.Year != issueDate.Year)
        {
            throw new InvoiceRuleException($"The number {number} belongs to {number.Year}, but the issue date is in {issueDate.Year}.");
        }

        if (dueDate < issueDate)
        {
            throw new InvoiceRuleException($"The due date {dueDate:yyyy-MM-dd} is before the issue date {issueDate:yyyy-MM-dd}.");
        }

        NumberYear = number.Year;
        NumberSequence = number.Sequence;
        IssueDate = issueDate;
        DueDate = dueDate;
        Status = InvoiceStatus.Issued;
        Revision++;
    }

    /// <summary>Records that the issued invoice was paid on the given date.</summary>
    /// <exception cref="InvoiceRuleException">The invoice is not issued, or the date is before the issue date.</exception>
    public void MarkPaid(DateOnly paidOn)
    {
        if (Status != InvoiceStatus.Issued)
        {
            throw new InvoiceRuleException($"Only an issued invoice can be marked as paid; this one is {Status}.");
        }

        if (paidOn < IssueDate)
        {
            throw new InvoiceRuleException($"The payment date {paidOn:yyyy-MM-dd} is before the issue date {IssueDate:yyyy-MM-dd}.");
        }

        PaidOn = paidOn;
        Status = InvoiceStatus.Paid;
        Revision++;
    }

    private void EnsureDraft(string action)
    {
        if (Status != InvoiceStatus.Draft)
        {
            string label = Number is null ? "this invoice" : $"invoice {Number}";
            throw new InvoiceRuleException($"Cannot {action} {label}: it is {Status}, and only a draft can change.");
        }
    }
}
