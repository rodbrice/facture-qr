namespace FactureQr.Core.Invoicing;

/// <summary>
/// Raised when an operation would break a rule of the invoice model: changing
/// an invoice that is no longer a draft, issuing an empty invoice, a total
/// outside the QR-bill amount range, a line with an invalid quantity or price.
/// The message explains the rule.
/// </summary>
public sealed class InvoiceRuleException : Exception
{
    /// <summary>Creates an exception without a message.</summary>
    public InvoiceRuleException()
    {
    }

    /// <summary>Creates an exception that explains the broken rule.</summary>
    public InvoiceRuleException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and an inner exception.</summary>
    public InvoiceRuleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
