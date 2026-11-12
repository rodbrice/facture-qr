using Codecrete.SwissQRBill.Generator;
using FactureQr.Core.Billing;

namespace FactureQr.Core.Tests;

public class IbanTests
{
    [Theory]
    [InlineData("CH44 3199 9123 0008 8901 2", true)]   // IG v2.4 Annex A example 2: QR-IID 31999
    [InlineData("CH64 3196 1000 0044 2155 7", true)]   // IG v2.4 Annex A example 1: QR-IID 31961
    [InlineData("CH58 0079 1123 0008 8901 2", false)]  // IG v2.4 Annex A example 4: IID 00791
    [InlineData("CH52 0483 5012 3456 7100 0", false)]  // IG v2.4 Annex A example 3: IID 04835
    [InlineData("ch5800791123000889012", false)]
    public void Parses_valid_ibans_and_detects_qr_ibans(string text, bool isQrIban)
    {
        Iban iban = Iban.Parse(text);

        Assert.Equal(21, iban.Value.Length);
        Assert.Equal(isQrIban, iban.IsQrIban);
        Assert.Equal(isQrIban, Payments.IsQrIban(iban.Value));
        Assert.True(Payments.IsValidIban(iban.Value));
    }

    [Theory]
    [InlineData("CH44 3199 9123 0008 8901 3", "check digits")]
    [InlineData("CH44 3199 9123 0008 8901", "21 characters")]
    [InlineData("DE89 3704 0044 0532 0130 00", "21 characters")]
    [InlineData("DE44 3199 9123 0008 8901 2", "CH or LI")]
    [InlineData("CH44 3199 9123 0008 890A 2", "digits")]
    [InlineData("", "21 characters")]
    public void Rejects_invalid_ibans(string text, string expectedReason)
    {
        var e = Assert.Throws<QrBillValidationException>(() => Iban.Parse(text));

        Assert.Contains(expectedReason, e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Formats_in_groups_of_four()
    {
        Assert.Equal("CH44 3199 9123 0008 8901 2", Iban.Parse("CH4431999123000889012").Formatted);
    }
}
