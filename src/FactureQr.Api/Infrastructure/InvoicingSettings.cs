using FactureQr.Core.Billing;
using FactureQr.Core.Invoicing;

namespace FactureQr.Api.Infrastructure;

/// <summary>
/// The configuration of the independent who issues the invoices, read from the
/// <c>Invoicing</c> section: the creditor (account and structured address).
/// </summary>
/// <param name="Creditor">The account and address printed on every payment part.</param>
public sealed record InvoicingSettings(CreditorAccount Creditor)
{
    /// <summary>Reads and validates the <c>Invoicing</c> section.</summary>
    /// <exception cref="InvalidOperationException">A value is missing or breaks a QR-bill rule; the message names the setting.</exception>
    public static InvoicingSettings Load(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection creditor = configuration.GetSection("Invoicing").GetSection("Creditor");
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

            return new InvoicingSettings(new CreditorAccount(account, address));
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
