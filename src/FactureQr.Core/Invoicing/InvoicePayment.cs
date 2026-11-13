using System.Globalization;
using FactureQr.Core.Billing;

namespace FactureQr.Core.Invoicing;

/// <summary>
/// Turns an issued invoice into the QR-bill that asks for its payment: the
/// creditor's account and address, the invoice total, the address copied onto
/// the invoice as the debtor, a reference derived from the invoice number and
/// the message "Facture &lt;number&gt;".
/// </summary>
public static class InvoicePayment
{
    /// <summary>
    /// Builds the QR-bill request of an issued or paid invoice.
    /// <para>
    /// The reference follows the kind of account. With a QR-IBAN it is a QR
    /// reference whose digits are the year and the sequence of the number
    /// (<c>2026-0042</c> gives <c>20260000042</c>, padded and with its check
    /// digit). With a plain IBAN it is a Creditor Reference with the same
    /// digits (<c>RF.. 2026 0000 042</c>). The sequence takes seven digits, so
    /// different numbers always give different references.
    /// </para>
    /// </summary>
    /// <exception cref="InvoiceRuleException">The invoice is a draft: it has no number yet.</exception>
    /// <exception cref="QrBillValidationException">The data breaks a rule of the QR-bill.</exception>
    public static QrBillRequest CreateRequest(Invoice invoice, CreditorAccount creditor)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(creditor);

        InvoiceNumber number = invoice.Number
            ?? throw new InvoiceRuleException("A draft has no number yet; only an issued invoice has a payment part.");

        string digits = string.Create(CultureInfo.InvariantCulture, $"{number.Year:D4}{number.Sequence:D7}");
        PaymentReference reference = creditor.Account.IsQrIban
            ? PaymentReference.CreateQrReference(digits)
            : PaymentReference.CreateCreditorReference(digits);

        return new QrBillRequest(
            creditor.Account,
            creditor.Address,
            reference,
            amount: invoice.Total,
            debtor: invoice.BillTo,
            message: $"Facture {number}");
    }
}
