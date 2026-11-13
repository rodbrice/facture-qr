using FactureQr.Core.Billing;

namespace FactureQr.Core.Invoicing;

/// <summary>
/// The independent who issues the invoices: the account that receives the
/// payments and the structured address printed as the creditor of the
/// payment part. The address must be the one of the account holder.
/// </summary>
/// <param name="Account">The IBAN or QR-IBAN that receives the payments.</param>
/// <param name="Address">The structured address of the account holder, limited to the characters the invoice PDF can print.</param>
/// <exception cref="QrBillValidationException">A field of the address contains a character the invoice PDF cannot print.</exception>
public sealed record CreditorAccount(Iban Account, StructuredAddress Address)
{
    /// <summary>The structured address of the account holder.</summary>
    public StructuredAddress Address { get; } = Printable(Address);

    private static StructuredAddress Printable(StructuredAddress address)
    {
        InvoicePdf.EnsurePrintable(address);
        return address;
    }
}
