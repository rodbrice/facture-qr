using FactureQr.Core.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactureQr.Persistence.Configurations;

internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines", table =>
        {
            table.HasCheckConstraint("ck_invoice_lines_quantity", "quantity > 0");
            table.HasCheckConstraint("ck_invoice_lines_unit_price", "unit_price >= 0");
            table.HasCheckConstraint("ck_invoice_lines_amount", "amount >= 0");
        });

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Description).HasMaxLength(InvoiceLine.MaxDescriptionLength);

        // numeric(9, 3) holds every quantity up to 999 999.999.
        builder.Property(l => l.Quantity).HasPrecision(9, InvoiceLine.QuantityDecimals);
        builder.Property(l => l.UnitPrice).HasPrecision(InvoiceConfiguration.AmountPrecision, InvoiceConfiguration.AmountScale);
        builder.Property(l => l.Amount).HasPrecision(InvoiceConfiguration.AmountPrecision, InvoiceConfiguration.AmountScale);

        builder.HasIndex("InvoiceId", nameof(InvoiceLine.Position)).IsUnique();
    }
}
