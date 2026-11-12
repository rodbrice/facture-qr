using FactureQr.Core.Billing;
using FactureQr.Core.Invoicing;

namespace FactureQr.Api.Infrastructure;

/// <summary>
/// The configuration of the independent who issues the invoices, read from the
/// <c>Invoicing</c> section: the creditor (account and structured address) and
/// the payment term used when an invoice is issued without a due date.
/// </summary>
/// <param name="Creditor">The account and address printed on every payment part.</param>
/// <param name="PaymentTermDays">Days between the issue date and the default due date.</param>
public sealed record InvoicingSettings(CreditorAccount Creditor, int PaymentTermDays)
{
    /// <summary>Payment term when none is configured.</summary>
    public const int DefaultPaymentTermDays = 30;

    /// <summary>Reads and validates the <c>Invoicing</c> section.</summary>
    /// <exception cref="InvalidOperationException">A value is missing or breaks a QR-bill rule; the message names the setting.</exception>
    public static InvoicingSettings Load(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection("Invoicing");
        IConfigurationSection creditor = section.GetSection("Creditor");
        try
        {
            var account = Iban.Parse(Required(creditor, "Iban"));
            var address = new StructuredAddress(
                Required(creditor, "Name"),
                creditor["Street"],
                creditor["BuildingNumber"],
                Required(creditor, "PostalCode"),
                Required(creditor, "Town"),
                Required(creditor, "Country"));

            int term = section.GetValue("PaymentTermDays", DefaultPaymentTermDays);
            if (term is < 0 or > 365)
            {
                throw new InvalidOperationException($"Invoicing:PaymentTermDays must be between 0 and 365 (got {term}).");
            }

            return new InvoicingSettings(new CreditorAccount(account, address), term);
        }
        catch (QrBillValidationException e)
        {
            throw new InvalidOperationException($"The creditor in the configuration section Invoicing:Creditor is invalid. {e.Message}", e);
        }
    }

    private static string Required(IConfigurationSection section, string key) =>
        section[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"The setting {section.Path}:{key} is required.");
}
