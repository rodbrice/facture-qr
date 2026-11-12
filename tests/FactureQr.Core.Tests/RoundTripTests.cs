using FactureQr.Core.Billing;
using FactureQr.Core.Samples;

namespace FactureQr.Core.Tests;

public class RoundTripTests
{
    public static TheoryData<string> SampleIds => new(SyntheticSamples.All.Select(s => s.Id));

    [Theory]
    [MemberData(nameof(SampleIds))]
    public void Decoding_the_generated_text_returns_the_input(string id)
    {
        QrBillRequest request = SyntheticSamples.All.Single(s => s.Id == id).Request;

        QrBillRequest decoded = QrBillRenderer.DecodeQrCodeText(QrBillRenderer.EncodeQrCodeText(request));

        Assert.Equal(request, decoded);
    }

    [Fact]
    public void Round_trip_without_amount_debtor_or_message()
    {
        var request = new QrBillRequest(
            Iban.Parse("CH52 0483 5012 3456 7100 0"),
            new StructuredAddress("Muster Stiftung", "P.O. box", null, "3001", "Bern", "CH"),
            PaymentReference.None);

        QrBillRequest decoded = QrBillRenderer.DecodeQrCodeText(QrBillRenderer.EncodeQrCodeText(request));

        Assert.Equal(request, decoded);
        Assert.Null(decoded.Amount);
        Assert.Null(decoded.Debtor);
        Assert.Null(decoded.Message);
    }

    [Fact]
    public void Round_trip_keeps_extended_latin_characters()
    {
        var request = new QrBillRequest(
            Iban.Parse("CH58 0079 1123 0008 8901 2"),
            new StructuredAddress("Ștefan Țărmure & Cie", "Rue de l'Église", "12b", "1700", "Fribourg", "CH"),
            PaymentReference.CreateCreditorReference("FACT2026001"),
            amount: 1500.00m,
            message: "Facture n° 2026-001, merci - 10 € d'escompte");

        QrBillRequest decoded = QrBillRenderer.DecodeQrCodeText(QrBillRenderer.EncodeQrCodeText(request));

        Assert.Equal(request, decoded);
    }

    [Fact]
    public void Decoding_a_EUR_bill_is_refused_by_our_currency_rule()
    {
        string eurText = QrBillRenderer.EncodeQrCodeText(SyntheticSamples.B).Replace("\nCHF\n", "\nEUR\n", StringComparison.Ordinal);

        var e = Assert.Throws<QrBillValidationException>(() => QrBillRenderer.DecodeQrCodeText(eurText));

        Assert.Equal("Currency", e.Field);
    }

    [Fact]
    public void Decoding_garbage_is_refused()
    {
        var e = Assert.Throws<QrBillValidationException>(() => QrBillRenderer.DecodeQrCodeText("not a QR-bill"));

        Assert.Equal("QR code text", e.Field);
    }
}
