using FactureQr.Core.Billing;

namespace FactureQr.Core.Tests;

public class CurrencyRuleTests
{
    private static readonly Iban QrIban = Iban.Parse("CH44 3199 9123 0008 8901 2");
    private static readonly Iban PlainIban = Iban.Parse("CH58 0079 1123 0008 8901 2");
    private static readonly StructuredAddress Creditor = new("Max Muster & Söhne", "Musterstrasse", "123", "8000", "Seldwyla", "CH");
    private static readonly PaymentReference Qrr = PaymentReference.QrReference("210000000003139471430009017");
    private static readonly PaymentReference Scor = PaymentReference.CreditorReference("RF18539007547034");

    [Theory]
    [InlineData("EUR")]
    [InlineData("eur")]
    [InlineData("USD")]
    [InlineData("chf")]
    [InlineData(" CHF ")]
    [InlineData("")]
    public void EUR_and_any_other_currency_are_refused_with_a_clear_message(string currency)
    {
        var e = Assert.Throws<QrBillValidationException>(() => new QrBillRequest(PlainIban, Creditor, Scor, 10m, currency: currency));

        Assert.Equal("Currency", e.Field);
        Assert.Contains("only CHF is supported", e.Message, StringComparison.Ordinal);
        Assert.Contains("ADR-0001", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_QR_IBAN_without_a_QR_reference_is_refused()
    {
        var withScor = Assert.Throws<QrBillValidationException>(() => new QrBillRequest(QrIban, Creditor, Scor, 10m));
        var withNone = Assert.Throws<QrBillValidationException>(() => new QrBillRequest(QrIban, Creditor, PaymentReference.None, 10m));

        Assert.Contains("QR-IBAN and requires a QR reference", withScor.Message, StringComparison.Ordinal);
        Assert.Contains("QR-IBAN and requires a QR reference", withNone.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_plain_IBAN_with_a_QR_reference_is_refused()
    {
        var e = Assert.Throws<QrBillValidationException>(() => new QrBillRequest(PlainIban, Creditor, Qrr, 10m));

        Assert.Contains("plain IBAN and cannot carry a QR reference", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Valid_combinations_are_accepted()
    {
        Assert.Equal(Qrr, new QrBillRequest(QrIban, Creditor, Qrr).Reference);
        Assert.Equal(Scor, new QrBillRequest(PlainIban, Creditor, Scor).Reference);
        Assert.Equal(PaymentReference.None, new QrBillRequest(PlainIban, Creditor, PaymentReference.None).Reference);
    }
}
