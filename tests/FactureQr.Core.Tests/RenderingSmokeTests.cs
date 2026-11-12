using FactureQr.Core.Billing;
using FactureQr.Core.Samples;

namespace FactureQr.Core.Tests;

public class RenderingSmokeTests
{
    public static TheoryData<string> SampleIds => new(SyntheticSamples.All.Select(s => s.Id));

    [Theory]
    [MemberData(nameof(SampleIds))]
    public void Pdf_starts_with_the_pdf_signature(string id)
    {
        QrBillRequest request = SyntheticSamples.All.Single(s => s.Id == id).Request;

        byte[] pdf = QrBillRenderer.RenderPdf(request);

        Assert.True(pdf.Length > 0);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void Pdf_output_is_deterministic()
    {
        Assert.Equal(QrBillRenderer.RenderPdf(SyntheticSamples.A), QrBillRenderer.RenderPdf(SyntheticSamples.A));
    }

    [Fact]
    public void Pdf_can_be_rendered_in_every_language()
    {
        foreach (BillLanguage language in Enum.GetValues<BillLanguage>())
        {
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(QrBillRenderer.RenderPdf(SyntheticSamples.A, language), 0, 4));
        }
    }

    [Fact]
    public void Qr_code_png_starts_with_the_png_signature()
    {
        byte[] png = QrBillRenderer.RenderQrCodePng(SyntheticSamples.A);

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4));
    }

    [Fact]
    public void Payment_part_png_starts_with_the_png_signature()
    {
        byte[] png = QrBillRenderer.RenderPng(SyntheticSamples.A);

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4));
    }
}
