using FactureQr.Core.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactureQr.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    // numeric(11, 2) holds every amount up to 999 999 999.99, the QR-bill maximum.
    internal const int AmountPrecision = 11;
    internal const int AmountScale = 2;

    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", table =>
        {
            table.HasCheckConstraint("ck_invoices_total_range", "total >= 0 AND total <= 999999999.99");
            table.HasCheckConstraint(
                "ck_invoices_issued_fields",
                "(status = 'Draft' AND number_year IS NULL AND number_sequence IS NULL AND issue_date IS NULL AND due_date IS NULL)"
                + " OR (status <> 'Draft' AND number_year IS NOT NULL AND number_sequence IS NOT NULL AND issue_date IS NOT NULL AND due_date IS NOT NULL"
                + " AND number_year = EXTRACT(YEAR FROM issue_date) AND due_date >= issue_date AND total >= 0.01)");
            table.HasCheckConstraint("ck_invoices_paid_on", "(status = 'Paid') = (paid_on IS NOT NULL)");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ComplexProperty(i => i.BillTo, AddressMapping.Configure);

        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(i => i.Total).HasPrecision(AmountPrecision, AmountScale);
        builder.Property(i => i.Revision).IsConcurrencyToken();

        builder.Ignore(i => i.Number);
        builder.HasIndex(i => new { i.NumberYear, i.NumberSequence })
            .IsUnique()
            .HasDatabaseName("ux_invoices_number");

        builder.HasMany(i => i.Lines)
            .WithOne()
            .HasForeignKey("InvoiceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
