using System.Net;
using FactureQr.Api.Invoices;
using FactureQr.Core.Billing;
using UglyToad.PdfPig;

namespace FactureQr.Api.Tests;

[Collection(PostgreSqlGroup.Name)]
public class InvoicePdfApiTests(PostgreSqlFixture postgres)
{
    [Fact]
    public async Task The_PDF_of_an_issued_invoice_carries_a_QR_bill_for_its_total_with_a_QR_reference()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync(), ApiFactory.QrIban);
        HttpClient client = api.CreateClient();
        InvoiceResponse invoice = await client.CreateIssuedAsync(new DateOnly(2026, 3, 2));

        HttpResponseMessage response = await client.GetAsync($"/invoices/{invoice.Id}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("facture-2026-0001.pdf", response.Content.Headers.ContentDisposition?.FileNameStar);
        byte[] pdf = await response.Content.ReadAsByteArrayAsync();

        // The QR code is read from the drawing in the PDF by an independent decoder, then parsed.
        QrBillRequest bill = QrBillRenderer.DecodeQrCodeText(PdfQrReader.ReadQrText(pdf));
        Assert.Equal(Iban.Parse(ApiFactory.QrIban), bill.Account);
        Assert.Equal("Max Muster & Söhne", bill.Creditor.Name);
        Assert.Equal(1200m, bill.Amount);
        Assert.Equal("CHF", bill.Currency);
        Assert.Equal(PaymentReference.CreateQrReference("20260000001"), bill.Reference);
        Assert.Equal(invoice.BillTo.ToStructuredAddress(), bill.Debtor);
        Assert.Equal("Facture 2026-0001", bill.Message);
    }

    [Fact]
    public async Task With_a_plain_IBAN_the_QR_bill_carries_a_Creditor_Reference()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync(), ApiFactory.PlainIban);
        HttpClient client = api.CreateClient();
        InvoiceResponse invoice = await client.CreateIssuedAsync(new DateOnly(2026, 3, 2));

        byte[] pdf = await client.GetByteArrayAsync($"/invoices/{invoice.Id}/pdf");

        QrBillRequest bill = QrBillRenderer.DecodeQrCodeText(PdfQrReader.ReadQrText(pdf));
        Assert.Equal(Iban.Parse(ApiFactory.PlainIban), bill.Account);
        Assert.Equal(PaymentReferenceType.CreditorReference, bill.Reference.Type);
        Assert.Equal(PaymentReference.CreateCreditorReference("20260000001"), bill.Reference);
    }

    [Fact]
    public async Task The_PDF_prints_the_number_the_dates_the_lines_and_the_total()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        InvoiceResponse invoice = await client.CreateIssuedAsync(new DateOnly(2026, 3, 2), new DateOnly(2026, 4, 1));

        byte[] pdf = await client.GetByteArrayAsync($"/invoices/{invoice.Id}/pdf");

        using PdfDocument document = PdfDocument.Open(pdf);
        Assert.Equal(1, document.NumberOfPages);
        string text = string.Join(' ', document.GetPage(1).GetWords().Select(w => w.Text));
        Assert.Contains("Facture 2026-0001", text);
        Assert.Contains("02.03.2026", text);
        Assert.Contains("01.04.2026", text);
        Assert.Contains("Analyse", text);
        Assert.Contains("Développement", text);
        Assert.Contains("1'200.00", text);
        Assert.Contains("Simon Muster", text);
    }

    [Fact]
    public async Task The_PDF_of_a_paid_invoice_is_still_available_and_says_it_was_paid()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        InvoiceResponse invoice = await client.CreateIssuedAsync(new DateOnly(2026, 3, 2));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/invoices/{invoice.Id}/mark-paid", null)).StatusCode);

        byte[] pdf = await client.GetByteArrayAsync($"/invoices/{invoice.Id}/pdf");

        using PdfDocument document = PdfDocument.Open(pdf);
        Assert.Contains("Payée le 12.11.2026", string.Join(' ', document.GetPage(1).GetWords().Select(w => w.Text)));
    }
}
