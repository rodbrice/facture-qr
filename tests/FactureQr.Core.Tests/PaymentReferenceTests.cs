using Codecrete.SwissQRBill.Generator;
using FactureQr.Core.Billing;

namespace FactureQr.Core.Tests;

public class PaymentReferenceTests
{
    // Valid QR references: IG v2.4 Annex A examples 1 and 2, and the Style Guide example.
    [Theory]
    [InlineData("000008207791225857421286694")]
    [InlineData("210000000003139471430009017")]
    [InlineData("21 00000 00003 13947 14300 09017")]
    public void QrReference_accepts_valid_references(string reference)
    {
        PaymentReference result = PaymentReference.QrReference(reference);

        Assert.Equal(PaymentReferenceType.QrReference, result.Type);
        Assert.Equal(27, result.Value.Length);
        Assert.Equal("QRR", result.TypeCode);
    }

    [Theory]
    [InlineData("210000000003139471430009018", "check digit")]   // last digit altered
    [InlineData("21000000000313947143000901", "27 digits")]      // 26 digits: too short
    [InlineData("2100000000031394714300090170", "27 digits")]    // 28 digits: too long
    [InlineData("21000000000313947143000901A", "only digits")]   // letter
    [InlineData("000000000000000000000000000", "zeros")]         // all zeros
    public void QrReference_rejects_invalid_references(string reference, string expectedReason)
    {
        var e = Assert.Throws<QrBillValidationException>(() => PaymentReference.QrReference(reference));

        Assert.Contains(expectedReason, e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateQrReference_pads_and_appends_the_check_digit()
    {
        // IG v2.4 Annex B example: 21 00000 00003 13947 14300 0901 -> check digit 7.
        PaymentReference fromAnnexB = PaymentReference.CreateQrReference("21000000000313947143000901");
        PaymentReference fromShortNumber = PaymentReference.CreateQrReference("1");

        Assert.Equal("210000000003139471430009017", fromAnnexB.Value);
        Assert.Equal("000000000000000000000000011", fromShortNumber.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789012345678901234567")] // 27 digits: a check digit would make 28
    [InlineData("12AB")]
    public void CreateQrReference_rejects_bad_input(string digits)
    {
        Assert.Throws<QrBillValidationException>(() => PaymentReference.CreateQrReference(digits));
    }

    [Theory]
    [InlineData("21000000000313947143000901", 7)] // IG v2.4 Annex B
    [InlineData("00000820779122585742128669", 4)] // IG v2.4 Annex A, example 1
    [InlineData("", 0)]
    public void Modulo10RecursiveCheckDigit_matches_the_guidelines(string digits, int expected)
    {
        Assert.Equal(expected, PaymentReference.Modulo10RecursiveCheckDigit(digits));
    }

    // Valid Creditor References: IG v2.4 Annex A examples 5 and 6, and the corrected example 4 (see docs/validation/B0-norma.md).
    [Theory]
    [InlineData("RF18539007547034")]
    [InlineData("RF18 5390 0754 7034")]
    [InlineData("RF240191230100405JSH0438")]
    [InlineData("rf240191230100405jsh0438")]
    public void CreditorReference_accepts_valid_references(string reference)
    {
        PaymentReference result = PaymentReference.CreditorReference(reference);

        Assert.Equal(PaymentReferenceType.CreditorReference, result.Type);
        Assert.StartsWith("RF", result.Value, StringComparison.Ordinal);
        Assert.Equal("SCOR", result.TypeCode);
    }

    [Theory]
    [InlineData("RF720191230100405JSH0438", "check digits")]     // IG v2.4 Annex A example 4 as printed: wrong check digits
    [InlineData("RF18539007547035", "check digits")]             // last digit altered
    [InlineData("RF18", "between 5 and 25")]                     // too short
    [InlineData("RF18539007547034123456789A", "between 5 and 25")] // 26 characters: too long
    [InlineData("XX18539007547034", "starts with RF")]
    [InlineData("RF18-539007547034", "letters and digits")]
    [InlineData("RFAB539007547034", "numeric")]
    public void CreditorReference_rejects_invalid_references(string reference, string expectedReason)
    {
        var e = Assert.Throws<QrBillValidationException>(() => PaymentReference.CreditorReference(reference));

        Assert.Contains(expectedReason, e.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("539007547034", "RF18539007547034")]
    [InlineData("0191230100405JSH0438", "RF240191230100405JSH0438")]
    [InlineData("1", "RF741")]
    public void CreateCreditorReference_computes_the_check_digits(string body, string expected)
    {
        Assert.Equal(expected, PaymentReference.CreateCreditorReference(body).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890123456789012")] // 22 characters: too long
    [InlineData("ABC 123")]                // spaces are removed, but
    [InlineData("ABC-123")]                // other characters are rejected
    public void CreateCreditorReference_rejects_bad_input(string body)
    {
        if (body == "ABC 123")
        {
            Assert.Equal("RF47ABC123", PaymentReference.CreateCreditorReference(body).Value);
            return;
        }

        Assert.Throws<QrBillValidationException>(() => PaymentReference.CreateCreditorReference(body));
    }

    [Fact]
    public void None_has_no_value_and_the_code_NON()
    {
        Assert.Equal(PaymentReferenceType.None, PaymentReference.None.Type);
        Assert.Equal(string.Empty, PaymentReference.None.Value);
        Assert.Equal("NON", PaymentReference.None.TypeCode);
        Assert.Equal(string.Empty, PaymentReference.None.Formatted);
    }

    [Fact]
    public void Formatted_groups_digits_as_printed_on_the_payment_part()
    {
        Assert.Equal("21 00000 00003 13947 14300 09017", PaymentReference.QrReference("210000000003139471430009017").Formatted);
        Assert.Equal("RF18 5390 0754 7034", PaymentReference.CreditorReference("RF18539007547034").Formatted);
        Assert.Equal("RF24 0191 2301 0040 5JSH 0438", PaymentReference.CreditorReference("RF240191230100405JSH0438").Formatted);
    }

    [Fact]
    public void Our_check_digits_agree_with_the_library_for_many_references()
    {
        // Deterministic sweep: the independent implementation and SwissQRBill.NET must agree.
        for (int i = 0; i < 500; i++)
        {
            string digits = (i * 7919L + 12345).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string qrr = PaymentReference.CreateQrReference(digits).Value;
            string scor = PaymentReference.CreateCreditorReference("INV" + digits).Value;

            Assert.True(Payments.IsValidQrReference(qrr), qrr);
            Assert.True(Payments.IsValidIso11649Reference(scor), scor);
            Assert.Equal(Payments.CreateQRReference(digits), qrr);
            Assert.Equal(Payments.CreateIso11649Reference("INV" + digits), scor);
        }
    }
}
