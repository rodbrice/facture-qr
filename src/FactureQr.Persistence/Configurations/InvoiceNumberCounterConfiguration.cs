using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactureQr.Persistence.Configurations;

internal sealed class InvoiceNumberCounterConfiguration : IEntityTypeConfiguration<InvoiceNumberCounter>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberCounter> builder)
    {
        builder.ToTable("invoice_number_counters", table =>
            table.HasCheckConstraint("ck_invoice_number_counters_last_sequence", "last_sequence >= 1"));
        builder.HasKey(c => c.Year);
        builder.Property(c => c.Year).ValueGeneratedNever();
    }
}
