using FactureQr.Core.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactureQr.Persistence.Configurations;

/// <summary>
/// Maps a <see cref="StructuredAddress"/> to columns of its owner's table, with
/// the lengths of the QR-bill standard. The record is rebuilt through its
/// constructor when it is read, so its validation runs again.
/// </summary>
internal static class AddressMapping
{
    public static void Configure(ComplexPropertyBuilder<StructuredAddress> address)
    {
        address.Property(a => a.Name).HasMaxLength(StructuredAddress.MaxNameLength).IsRequired();
        address.Property(a => a.Street).HasMaxLength(StructuredAddress.MaxStreetLength);
        address.Property(a => a.BuildingNumber).HasMaxLength(StructuredAddress.MaxBuildingNumberLength);
        address.Property(a => a.PostalCode).HasMaxLength(StructuredAddress.MaxPostalCodeLength).IsRequired();
        address.Property(a => a.Town).HasMaxLength(StructuredAddress.MaxTownLength).IsRequired();
        address.Property(a => a.Country).HasMaxLength(2).IsFixedLength().IsRequired();
    }
}
