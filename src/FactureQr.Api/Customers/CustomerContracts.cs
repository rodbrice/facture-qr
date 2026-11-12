using FactureQr.Core.Billing;
using FactureQr.Core.Invoicing;

namespace FactureQr.Api.Customers;

/// <summary>A structured address as the QR-bill requires it (no combined address lines).</summary>
/// <param name="Name">Name of the person or company, at most 70 characters.</param>
/// <param name="Street">Street or P.O. box, at most 70 characters.</param>
/// <param name="BuildingNumber">Building number, at most 16 characters.</param>
/// <param name="PostalCode">Postal code without country prefix, at most 16 characters.</param>
/// <param name="Town">Town, at most 35 characters.</param>
/// <param name="Country">Two-letter ISO 3166-1 country code, for example CH.</param>
public sealed record AddressDto(string Name, string? Street, string? BuildingNumber, string PostalCode, string Town, string Country)
{
    /// <summary>Copies a stored address.</summary>
    public static AddressDto From(StructuredAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return new(address.Name, address.Street, address.BuildingNumber, address.PostalCode, address.Town, address.Country);
    }

    /// <summary>Validates the address with the QR-bill rules.</summary>
    /// <exception cref="QrBillValidationException">A field breaks a rule; the exception names it.</exception>
    public StructuredAddress ToStructuredAddress() => new(Name, Street, BuildingNumber, PostalCode, Town, Country);
}

/// <summary>A customer.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Address">The structured address, including the name.</param>
public sealed record CustomerResponse(Guid Id, AddressDto Address)
{
    /// <summary>Copies a stored customer.</summary>
    public static CustomerResponse From(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        return new(customer.Id, AddressDto.From(customer.Address));
    }
}
