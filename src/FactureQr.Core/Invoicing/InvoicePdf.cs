using System.Globalization;
using System.Text;
using Codecrete.SwissQRBill.Generator;
using Codecrete.SwissQRBill.Generator.Canvas;
using FactureQr.Core.Billing;

namespace FactureQr.Core.Invoicing;

/// <summary>
/// Renders an issued invoice as a one-page A4 PDF in French: the creditor and
/// the address the invoice is sent to at the top, the number and dates, the
/// lines and the total, and the QR-bill payment part with receipt across the
/// bottom 105 mm of the page (the position the Style Guide prescribes for an
/// A4 invoice). The payment part comes from <see cref="InvoicePayment"/> and
/// goes through the same validation as <see cref="QrBillRenderer"/>.
/// <para>
/// The page is not split across several pages in this version: an invoice
/// whose lines do not fit above the payment part is refused, and
/// <see cref="LinesFitOnOnePage"/> lets callers refuse such a line before it
/// is added.
/// </para>
/// </summary>
public static class InvoicePdf
{
    // Only the regular font is used: the library embeds a bold font that contains only the characters
    // of its own headings, so names or descriptions printed in bold could fail to render.
    // All positions are in millimetres from the bottom-left corner of the page.
    private const double PageWidth = 210;
    private const double PageHeight = 297;
    private const double PaymentPartHeight = 105;
    private const double Left = 20;
    private const double Right = 190;
    private const double AddressColumn = 115;

    // Columns of the line table: the description wraps inside its column, the numbers are right-aligned.
    private const double DescriptionWidth = 95;
    private const double QuantityRight = 135;
    private const double UnitPriceRight = 162;
    private const double AmountRight = Right;

    private const int TextSize = 10;
    private const int TitleSize = 14;
    private const double TableTop = 200;
    private const double TableBottom = PaymentPartHeight + 15;
    private const double RowGap = 1.2;

    // Swiss notation for amounts: apostrophe between thousands, point before the centimes (1'204.50).
    private static readonly NumberFormatInfo SwissAmounts = new() { NumberGroupSeparator = "'", NumberDecimalSeparator = "." };

    /// <summary>
    /// Renders the invoice with its payment part.
    /// </summary>
    /// <exception cref="InvoiceRuleException">The invoice is a draft, or its lines do not fit on one page.</exception>
    /// <exception cref="QrBillValidationException">The payment part breaks a rule of the QR-bill.</exception>
    public static byte[] Render(Invoice invoice, CreditorAccount creditor)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(creditor);

        QrBillRequest request = InvoicePayment.CreateRequest(invoice, creditor);
        Bill bill = QrBillRenderer.ValidatedBill(request, GraphicsFormat.PDF, OutputSize.A4PortraitSheet, 0);

        using var canvas = (AbstractCanvas)CanvasCreator.Create(bill.Format, bill.CharacterSet, PageWidth, PageHeight);
        if (!LinesFit(canvas, invoice.Lines))
        {
            throw new InvoiceRuleException("The lines of this invoice do not fit on one page.");
        }

        DrawHeader(canvas, invoice, creditor.Address);
        DrawLines(canvas, invoice);
        QRBill.Draw(bill, canvas);
        return canvas.ToByteArray();
    }

    /// <summary>
    /// True when the regular font of the PDF contains the character: Basic Latin,
    /// Latin-1 Supplement except ÿ, Latin Extended-A, Ș ș Ț ț and €. Any other
    /// character makes <see cref="Render"/> fail, so text that will be printed is
    /// checked with <see cref="FirstUnprintable"/> before it is stored.
    /// </summary>
    public static bool CanPrint(Rune character) =>
        character.Value is (>= 0x20 and <= 0x7E)
            or (>= 0xA0 and <= 0xFE)
            or (>= 0x100 and <= 0x17F)
            or (>= 0x218 and <= 0x21B)
            or 0x20AC;

    /// <summary>
    /// The first character of the text the regular font of the PDF cannot print,
    /// or null when the whole text can be printed.
    /// </summary>
    public static Rune? FirstUnprintable(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (Rune character in text.EnumerateRunes())
        {
            if (!CanPrint(character))
            {
                return character;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks that every field of an address can be printed on the PDF. The
    /// character set of the QR-bill is wider than the font by one character, ÿ,
    /// so an address the QR code accepts can still make <see cref="Render"/> fail.
    /// </summary>
    /// <exception cref="QrBillValidationException">A field contains a character the font cannot print; the exception names the field.</exception>
    public static void EnsurePrintable(StructuredAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        (string Field, string? Text)[] fields =
        [
            ("Name", address.Name),
            ("Street", address.Street),
            ("Building number", address.BuildingNumber),
            ("Postal code", address.PostalCode),
            ("Town", address.Town),
        ];
        foreach ((string field, string? text) in fields)
        {
            if (text is not null && FirstUnprintable(text) is Rune character)
            {
                throw new QrBillValidationException(field, $"the character '{character}' (U+{character.Value:X4}) cannot be printed on the invoice PDF.");
            }
        }
    }

    /// <summary>
    /// True when the lines fit in the table of one page above the payment part.
    /// Descriptions wrap inside their column, so the answer depends on their
    /// length as printed, not on the number of lines alone.
    /// </summary>
    public static bool LinesFitOnOnePage(IReadOnlyList<InvoiceLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var format = new BillFormat { GraphicsFormat = GraphicsFormat.PDF, OutputSize = OutputSize.A4PortraitSheet };
        using var canvas = (AbstractCanvas)CanvasCreator.Create(format, SpsCharacterSet.ExtendedLatin, PageWidth, PageHeight);
        return LinesFit(canvas, lines);
    }

    private static bool LinesFit(AbstractCanvas canvas, IReadOnlyList<InvoiceLine> lines)
    {
        double rows = lines.Sum(l => RowHeight(canvas, l));
        return TableTop - HeaderRowHeight(canvas) - rows >= TableBottom;
    }

    private static void DrawHeader(AbstractCanvas canvas, Invoice invoice, StructuredAddress creditor)
    {
        double y = PageHeight - 25;
        canvas.PutText(creditor.Name, Left, y, TextSize, false);
        DrawAddressLines(canvas, creditor, Left, y - canvas.LineHeight(TextSize));

        double billToTop = PageHeight - 55;
        canvas.PutText(invoice.BillTo.Name, AddressColumn, billToTop, TextSize, false);
        DrawAddressLines(canvas, invoice.BillTo, AddressColumn, billToTop - canvas.LineHeight(TextSize));

        double titleY = TableTop + 25;
        canvas.PutText($"Facture {invoice.Number}", Left, titleY, TitleSize, false);

        double line = canvas.LineHeight(TextSize);
        double infoY = titleY - (line * 2);
        canvas.PutText($"Date : {FormatDate(invoice.IssueDate)}", Left, infoY, TextSize, false);
        canvas.PutText($"Payable jusqu'au : {FormatDate(invoice.DueDate)}", Left, infoY - line, TextSize, false);
        if (invoice.PaidOn is DateOnly paidOn)
        {
            canvas.PutText($"Payée le {FormatDate(paidOn)}", AddressColumn, infoY, TextSize, false);
        }
    }

    private static void DrawAddressLines(AbstractCanvas canvas, StructuredAddress address, double x, double y)
    {
        double line = canvas.LineHeight(TextSize);
        string street = string.Join(' ', new[] { address.Street, address.BuildingNumber }.Where(s => !string.IsNullOrEmpty(s)));
        if (street.Length > 0)
        {
            canvas.PutText(street, x, y, TextSize, false);
            y -= line;
        }

        string town = address.Country == "CH" ? $"{address.PostalCode} {address.Town}" : $"{address.Country}-{address.PostalCode} {address.Town}";
        canvas.PutText(town, x, y, TextSize, false);
    }

    private static void DrawLines(AbstractCanvas canvas, Invoice invoice)
    {
        double y = TableTop;
        canvas.PutText("Description", Left, y, TextSize, false);
        PutRight(canvas, "Quantité", QuantityRight, y);
        PutRight(canvas, "Prix unitaire", UnitPriceRight, y);
        PutRight(canvas, "Montant CHF", AmountRight, y);
        y -= HeaderRowHeight(canvas);
        DrawRule(canvas, y + (canvas.LineHeight(TextSize) * 0.75) + (RowGap / 2));

        foreach (InvoiceLine item in invoice.Lines)
        {
            string[] description = Wrap(canvas, item.Description);
            canvas.PutTextLines(description, Left, y, TextSize, 0);
            PutRight(canvas, item.Quantity.ToString("0.###", CultureInfo.InvariantCulture), QuantityRight, y);
            PutRight(canvas, FormatAmount(item.UnitPrice), UnitPriceRight, y);
            PutRight(canvas, FormatAmount(item.Amount), AmountRight, y);
            y -= RowHeight(canvas, item);
        }

        DrawRule(canvas, y + (canvas.LineHeight(TextSize) * 0.75) + (RowGap / 2));
        canvas.PutText("Total CHF", Left, y, TextSize, false);
        PutRight(canvas, FormatAmount(invoice.Total), AmountRight, y);
    }

    private static double HeaderRowHeight(AbstractCanvas canvas) => canvas.LineHeight(TextSize) + (RowGap * 2);

    private static double RowHeight(AbstractCanvas canvas, InvoiceLine line) =>
        (Wrap(canvas, line.Description).Length * canvas.LineHeight(TextSize)) + RowGap;

    /// <summary>
    /// Splits a description into lines no wider than the description column:
    /// words are added while the line still fits; a word longer than the
    /// column on its own is cut between characters.
    /// </summary>
    private static string[] Wrap(AbstractCanvas canvas, string text)
    {
        var lines = new List<string>();
        string current = string.Empty;
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = current.Length == 0 ? word : $"{current} {word}";
            if (Fits(canvas, candidate))
            {
                current = candidate;
                continue;
            }

            if (current.Length > 0)
            {
                lines.Add(current);
            }

            current = word;
            while (!Fits(canvas, current))
            {
                int cut = current.Length - 1;
                while (cut > 1 && !Fits(canvas, current[..cut]))
                {
                    cut--;
                }

                lines.Add(current[..cut]);
                current = current[cut..];
            }
        }

        lines.Add(current);
        return [.. lines];
    }

    private static bool Fits(AbstractCanvas canvas, string text) => canvas.TextWidth(text, TextSize, false) <= DescriptionWidth;

    private static void PutRight(AbstractCanvas canvas, string text, double right, double y) =>
        canvas.PutText(text, right - canvas.TextWidth(text, TextSize, false), y, TextSize, false);

    private static void DrawRule(AbstractCanvas canvas, double y)
    {
        canvas.StartPath();
        canvas.MoveTo(Left, y);
        canvas.LineTo(Right, y);
        canvas.StrokePath(0.3, 0x000000, LineStyle.Solid, false);
    }

    private static string FormatAmount(decimal amount) => amount.ToString("#,##0.00", SwissAmounts);

    private static string FormatDate(DateOnly? date) => date?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
}
