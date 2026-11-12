using FactureQr.Core.Billing;

namespace FactureQr.Core.Invoicing;

/// <summary>
/// A customer of the independent. The address is a QR-bill structured
/// address, so it can be printed as the debtor of the payment part.
/// </summary>
public sealed class Customer
{
    /// <summary>Creates a customer with a new identifier.</summary>
    public Customer(StructuredAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

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
    public void ChangeAddress(StructuredAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        Address = address;
    }
}
