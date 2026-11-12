using FactureQr.Core.Billing;
using FactureQr.Core.Samples;

namespace FactureQr.Core.Tests;

public class QrBillRequestTests
{
    private static readonly Iban PlainIban = Iban.Parse("CH58 0079 1123 0008 8901 2");
    private static readonly StructuredAddress Creditor = new("Max Muster & Söhne", "Musterstrasse", "123", "8000", "Seldwyla", "CH");

    [Theory]
    [InlineData(0.01)]
    [InlineData(1949.75)]
    [InlineData(999999999.99)]
    public void Accepts_amounts_within_the_permitted_range(decimal amount)
    {
        var request = new QrBillRequest(PlainIban, Creditor, PaymentReference.None, amount);

        Assert.Equal(amount, request.Amount);
    }

    [Theory]
    [InlineData(0.00)]
    [InlineData(0.001)]
    [InlineData(1.005)]
    [InlineData(1000000000.00)]
    [InlineData(-5.00)]
    public void Rejects_amounts_outside_the_range_or_with_more_than_two_decimals(decimal amount)
    {
        var e = Assert.Throws<QrBillValidationException>(() => new QrBillRequest(PlainIban, Creditor, PaymentReference.None, amount));

        Assert.Equal("Amount", e.Field);
    }

    [Fact]
    public void Amount_debtor_and_message_are_optional()
    {
        var request = new QrBillRequest(PlainIban, Creditor, PaymentReference.None);

        Assert.Null(request.Amount);
        Assert.Null(request.Debtor);
        Assert.Null(request.Message);
        Assert.Equal("CHF", request.Currency);
    }

    [Fact]
    public void Message_is_limited_to_140_characters_of_the_permitted_set()
    {
        Assert.Equal(140, new QrBillRequest(PlainIban, Creditor, PaymentReference.None, message: new string('m', 140)).Message!.Length);
        Assert.Throws<QrBillValidationException>(() => new QrBillRequest(PlainIban, Creditor, PaymentReference.None, message: new string('m', 141)));
        Assert.Throws<QrBillValidationException>(() => new QrBillRequest(PlainIban, Creditor, PaymentReference.None, message: "Спасибо"));
        Assert.Null(new QrBillRequest(PlainIban, Creditor, PaymentReference.None, message: "   ").Message);
    }

    [Fact]
    public void Samples_cover_the_three_reference_variants()
    {
        Assert.Equal(PaymentReferenceType.QrReference, SyntheticSamples.A.Reference.Type);
        Assert.True(SyntheticSamples.A.Account.IsQrIban);
        Assert.Equal(PaymentReferenceType.CreditorReference, SyntheticSamples.B.Reference.Type);
        Assert.False(SyntheticSamples.B.Account.IsQrIban);
        Assert.Equal(PaymentReferenceType.None, SyntheticSamples.C.Reference.Type);
        Assert.False(SyntheticSamples.C.Account.IsQrIban);
        Assert.Equal(["a", "b", "c"], SyntheticSamples.All.Select(s => s.Id));
    }
}
