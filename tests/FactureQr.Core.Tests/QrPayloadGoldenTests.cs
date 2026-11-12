using FactureQr.Core.Billing;
using FactureQr.Core.Samples;

namespace FactureQr.Core.Tests;

public class QrPayloadGoldenTests
{
    // Line numbers and names of the Swiss QR Code data structure (IG v2.4, chapter 4.2.2, Table 8).
    private static readonly string[] LineNames =
    [
        "1 QRType", "2 Version", "3 Coding",
        "4 IBAN",
        "5 Cdtr.AdrTp", "6 Cdtr.Name", "7 Cdtr.StrtNmOrAdrLine1", "8 Cdtr.BldgNbOrAdrLine2", "9 Cdtr.PstCd", "10 Cdtr.TwnNm", "11 Cdtr.Ctry",
        "12 UltmtCdtr.AdrTp", "13 UltmtCdtr.Name", "14 UltmtCdtr.StrtNmOrAdrLine1", "15 UltmtCdtr.BldgNbOrAdrLine2", "16 UltmtCdtr.PstCd", "17 UltmtCdtr.TwnNm", "18 UltmtCdtr.Ctry",
        "19 Amt", "20 Ccy",
        "21 UltmtDbtr.AdrTp", "22 UltmtDbtr.Name", "23 UltmtDbtr.StrtNmOrAdrLine1", "24 UltmtDbtr.BldgNbOrAdrLine2", "25 UltmtDbtr.PstCd", "26 UltmtDbtr.TwnNm", "27 UltmtDbtr.Ctry",
        "28 RmtInf.Tp", "29 RmtInf.Ref",
        "30 AddInf.Ustrd", "31 AddInf.Trailer",
    ];

    public static TheoryData<string> SampleIds => new(SyntheticSamples.All.Select(s => s.Id));

    [Theory]
    [MemberData(nameof(SampleIds))]
    public void Qr_code_text_matches_the_expected_file_line_by_line(string id)
    {
        QrBillRequest request = SyntheticSamples.All.Single(s => s.Id == id).Request;
        string expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"sample-{id}.expected.txt")).Replace("\r\n", "\n", StringComparison.Ordinal);

        string actual = QrBillRenderer.EncodeQrCodeText(request);

        string[] expectedLines = expected.Split('\n');
        string[] actualLines = actual.Split('\n');
        Assert.Equal(LineNames.Length, expectedLines.Length);
        Assert.Equal(LineNames.Length, actualLines.Length);
        for (int i = 0; i < LineNames.Length; i++)
        {
            Assert.True(expectedLines[i] == actualLines[i], $"Line {LineNames[i]}: expected '{expectedLines[i]}' but got '{actualLines[i]}'.");
        }

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Header_and_trailer_are_fixed()
    {
        string[] lines = QrBillRenderer.EncodeQrCodeText(SyntheticSamples.A).Split('\n');

        Assert.Equal("SPC", lines[0]);
        Assert.Equal("0200", lines[1]);
        Assert.Equal("1", lines[2]);
        Assert.Equal("EPD", lines[^1]);
        Assert.DoesNotContain('\r', QrBillRenderer.EncodeQrCodeText(SyntheticSamples.A));
    }
}
