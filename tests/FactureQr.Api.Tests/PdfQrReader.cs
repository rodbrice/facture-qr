using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace FactureQr.Api.Tests;

/// <summary>
/// Reads the Swiss QR Code back from a PDF without using the library that drew
/// it: the filled shapes of the payment part are painted, in drawing order,
/// onto a black-and-white bitmap, and the bitmap is decoded by ZXing.Net.
/// The QR-bill library draws the code as filled rectangles (the modules, then
/// the Swiss cross), so painting their bounding boxes reproduces the code.
/// </summary>
public static class PdfQrReader
{
    // The payment part is the bottom 105 mm of the A4 page; the QR code sits in its left half, after the 62 mm receipt.
    private const double PointsPerMillimetre = 72 / 25.4;
    private const double RegionLeft = 62 * PointsPerMillimetre;
    private const double RegionRight = 210 * PointsPerMillimetre;
    private const double RegionTop = 105 * PointsPerMillimetre;
    private const int PixelsPerPoint = 4;

    /// <summary>Returns the text of the QR code drawn in the payment part of the first page.</summary>
    public static string ReadQrText(byte[] pdf)
    {
        using PdfDocument document = PdfDocument.Open(pdf);
        Page page = document.GetPage(1);

        int width = (int)((RegionRight - RegionLeft) * PixelsPerPoint);
        int height = (int)(RegionTop * PixelsPerPoint);
        byte[] luminance = new byte[width * height];
        Array.Fill(luminance, (byte)255);

        foreach (PdfPath path in page.Paths.Where(p => p.IsFilled && !p.IsClipping))
        {
            byte shade = Shade(path);
            foreach (PdfSubpath subpath in path)
            {
                if (subpath.GetBoundingRectangle() is PdfRectangle box)
                {
                    Paint(luminance, width, height, box, shade);
                }
            }
        }

        var source = new RGBLuminanceSource(luminance, width, height, RGBLuminanceSource.BitmapFormat.Gray8);
        var hints = new Dictionary<DecodeHintType, object> { [DecodeHintType.TRY_HARDER] = true, [DecodeHintType.CHARACTER_SET] = "UTF-8" };
        Result? result = new QRCodeReader().decode(new BinaryBitmap(new HybridBinarizer(source)), hints);
        return result?.Text ?? throw new InvalidOperationException("No QR code was found in the payment part of the PDF.");
    }

    private static byte Shade(PdfPath path)
    {
        (double r, double g, double b) = path.FillColor?.ToRGBValues() ?? (0, 0, 0);
        return (r + g + b) / 3 < 0.5 ? (byte)0 : (byte)255;
    }

    private static void Paint(byte[] pixels, int width, int height, PdfRectangle box, byte shade)
    {
        int left = Math.Max(0, (int)Math.Round((box.Left - RegionLeft) * PixelsPerPoint));
        int right = Math.Min(width, (int)Math.Round((box.Right - RegionLeft) * PixelsPerPoint));
        int top = Math.Max(0, (int)Math.Round((RegionTop - box.Top) * PixelsPerPoint));
        int bottom = Math.Min(height, (int)Math.Round((RegionTop - box.Bottom) * PixelsPerPoint));
        for (int y = top; y < bottom; y++)
        {
            Array.Fill(pixels, shade, (y * width) + left, Math.Max(0, right - left));
        }
    }
}
