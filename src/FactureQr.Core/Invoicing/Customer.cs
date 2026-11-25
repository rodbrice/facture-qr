using FactureQr.Core.Billing;

namespace FactureQr.Core.Invoicing;

/// <summary>
/// A customer of the independent. The address is a QR-bill structured
/// address limited to the characters the invoice PDF can print, so it can be
/// printed as the debtor of the payment part.
/// </summary>
public sealed class Customer
{
    /// <summary>Creates a customer with a new identifier.</summary>
    /// <exception cref="QrBillValidationException">A field of the address contains a character the invoice PDF cannot print.</exception>
    public Customer(StructuredAddress address)
    {
        InvoicePdf.EnsurePrintable(address);

        Id = Guid.CreateVersion7();
        Address = address;
    }

    // Used by the persistence layer to materialise a stored customer.
    private Customer()
    {
        Address = null!;
    }

    /// <summary>The identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>The structured address, including the name.</summary>
    public StructuredAddress Address { get; private set; }

    /// <summary>
    /// Replaces the address. Invoices already created keep the address they
    /// copied when they were created.
    /// </summary>
    /// <exception cref="QrBillValidationException">A field of the address contains a character the invoice PDF cannot print.</exception>
    public void ChangeAddress(StructuredAddress address)
    {
        InvoicePdf.EnsurePrintable(address);

        Address = address;
    }
}
