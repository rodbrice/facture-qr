using FactureQr.Api.Customers;
using FactureQr.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FactureQr.Api.Settings;

/// <summary>The creditor printed on every invoice, as read from the configuration.</summary>
/// <param name="Address">The structured address of the account holder.</param>
/// <param name="Iban">The account, grouped by four characters (<c>CH44 3199 9123 0008 8901 2</c>).</param>
/// <param name="IsQrIban">True for a QR-IBAN (the invoices carry a QR reference), false for an IBAN (Creditor Reference).</param>
public sealed record CreditorResponse(AddressDto Address, string Iban, bool IsQrIban);

/// <summary>The <c>/settings</c> endpoints: read-only, the values come from the configuration.</summary>
public static class SettingsEndpoints
{
    /// <summary>Maps the endpoints.</summary>
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/settings").WithTags("Settings")
            .MapGet("/creditor", Creditor)
            .WithSummary("Returns the creditor from the Invoicing:Creditor configuration section.");

        return app;
    }

    private static Ok<CreditorResponse> Creditor(InvoicingSettings settings) =>
        TypedResults.Ok(new CreditorResponse(
            AddressDto.From(settings.Creditor.Address),
            settings.Creditor.Account.Formatted,
            settings.Creditor.Account.IsQrIban));
}
