using FactureQr.Core.Billing;
using FactureQr.Core.Invoicing;

namespace FactureQr.Core.Tests;

public class InvoicePaymentTests
{
    private static readonly StructuredAddress MaxMuster = new("Max Muster & Söhne", "Musterstrasse", "123", "8000", "Seldwyla", "CH");
    private static readonly StructuredAddress SimonMuster = new("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "CH");
    private static readonly CreditorAccount WithQrIban = new(Iban.Parse("CH44 3199 9123 0008 8901 2"), MaxMuster);
    private static readonly CreditorAccount WithIban = new(Iban.Parse("CH58 0079 1123 0008 8901 2"), MaxMuster);

    [Fact]
    public void A_QR_IBAN_gets_a_QR_reference_made_of_the_year_and_the_sequence()
    {
        Invoice invoice = Issued(new InvoiceNumber(2026, 42));

        QrBillRequest request = InvoicePayment.CreateRequest(invoice, WithQrIban);

        // 26 digits (the number, left-padded with zeros), then the check digit.
        Assert.Equal(PaymentReferenceType.QrReference, request.Reference.Type);
        Assert.Equal("00000000000000020260000042", request.Reference.Value[..26]);
        Assert.Equal(PaymentReference.CreateQrReference("20260000042"), request.Reference);
        Assert.Equal(invoice.Total, request.Amount);
        Assert.Equal(SimonMuster, request.Debtor);
        Assert.Equal(MaxMuster, request.Creditor);
        Assert.Equal("Facture 2026-0042", request.Message);
    }

    [Fact]
    public void A_plain_IBAN_gets_a_Creditor_Reference_with_the_same_digits()
    {
        QrBillRequest request = InvoicePayment.CreateRequest(Issued(new InvoiceNumber(2026, 42)), WithIban);

        Assert.Equal(PaymentReferenceType.CreditorReference, request.Reference.Type);
        Assert.EndsWith("20260000042", request.Reference.Value, StringComparison.Ordinal);
        Assert.Equal(PaymentReference.CreateCreditorReference("20260000042"), request.Reference);
    }

    [Fact]
    public void Different_numbers_give_different_references()
    {
        string[] references =
        [
            InvoicePayment.CreateRequest(Issued(new InvoiceNumber(2026, 1)), WithQrIban).Reference.Value,
            InvoicePayment.CreateRequest(Issued(new InvoiceNumber(2026, 10)), WithQrIban).Reference.Value,
            InvoicePayment.CreateRequest(Issued(new InvoiceNumber(2027, 1)), WithQrIban).Reference.Value,
            InvoicePayment.CreateRequest(Issued(new InvoiceNumber(2026, 9_999_999)), WithQrIban).Reference.Value,
        ];

        Assert.Equal(references.Length, references.Distinct().Count());
    }

    [Fact]
    public void A_draft_has_no_payment_part()
    {
        var draft = Invoice.CreateDraft(new Customer(SimonMuster));
        draft.AddLine("Analyse", 1m, 100m);

        Assert.Throws<InvoiceRuleException>(() => InvoicePayment.CreateRequest(draft, WithQrIban));
        Assert.Throws<InvoiceRuleException>(() => InvoicePdf.Render(draft, WithQrIban));
    }

    [Fact]
    public void The_PDF_is_one_A4_page()
    {
        byte[] pdf = InvoicePdf.Render(Issued(new InvoiceNumber(2026, 1)), WithQrIban);

        string text = System.Text.Encoding.Latin1.GetString(pdf);
        Assert.StartsWith("%PDF", text, StringComparison.Ordinal);
        Assert.Contains("/Count 1", text, StringComparison.Ordinal);
        Assert.Contains("/MediaBox [ 0 0 595.276 841.89 ]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Lines_fit_until_the_table_reaches_the_payment_part()
    {
        var draft = Invoice.CreateDraft(new Customer(SimonMuster));
        string description = new('W', InvoiceLine.MaxDescriptionLength);
        int added = 0;
        while (InvoicePdf.LinesFitOnOnePage(draft.Lines))
        {
            draft.AddLine(description, 1m, 1m);
            added++;
        }

        // Long descriptions wrap over several printed lines, so only a few such lines fit.
        Assert.InRange(added - 1, 1, 10);

        var shortLines = Invoice.CreateDraft(new Customer(SimonMuster));
        for (int i = 0; i < 10; i++)
        {
            shortLines.AddLine("Analyse", 1m, 1m);
        }

        Assert.True(InvoicePdf.LinesFitOnOnePage(shortLines.Lines));
    }

    private static Invoice Issued(InvoiceNumber number)
    {
        var invoice = Invoice.CreateDraft(new Customer(SimonMuster));
        invoice.AddLine("Analyse", 2m, 150m);
        var issueDate = new DateOnly(number.Year, 3, 2);
        invoice.Issue(number, issueDate, issueDate.AddDays(30));
        return invoice;
    }
}
