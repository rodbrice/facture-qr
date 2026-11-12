namespace FactureQr.Core.Invoicing;

/// <summary>
/// The stored states of an invoice. A draft can be edited; issuing gives it a
/// number and freezes it; an issued invoice can only be marked as paid.
/// "Overdue" is not a stored state: it is derived from the due date, see
/// <see cref="Invoice.IsOverdue"/>.
/// </summary>
public enum InvoiceStatus
{
    /// <summary>Being prepared: lines and customer can change; no number yet.</summary>
    Draft,

    /// <summary>Numbered and sent: its content can no longer change.</summary>
    Issued,

    /// <summary>Marked as paid: final.</summary>
    Paid,
}
