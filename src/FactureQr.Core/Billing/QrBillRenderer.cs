using Codecrete.SwissQRBill.Generator;

namespace FactureQr.Core.Billing;

/// <summary>
/// Turns a <see cref="QrBillRequest"/> into the Swiss QR Code text, a PDF of
/// the payment part with receipt, or a PNG of the QR code. This is the only
/// place that talks to the SwissQRBill.NET library (see ADR-0002): the
/// request is validated by our own rules first, then by the library, and only
/// then rendered.
/// </summary>
public static class QrBillRenderer
{
    /// <summary>Validates the request and returns the text encoded in the Swiss QR Code (31 lines separated by LF).</summary>
    /// <exception cref="QrBillValidationException">The library reports an error or a warning for the data.</exception>
    public static string EncodeQrCodeText(QrBillRequest request) =>
        QRBill.EncodeQrCodeText(ValidatedBill(request, GraphicsFormat.PDF, OutputSize.QrBillOnly, BillLanguage.French, 0));

    /// <summary>Validates the request and renders the payment part with receipt (210 × 105 mm) as a PDF.</summary>
    /// <exception cref="QrBillValidationException">The library reports an error or a warning for the data.</exception>
    public static byte[] RenderPdf(QrBillRequest request, BillLanguage language = BillLanguage.French) =>
        QRBill.Generate(ValidatedBill(request, GraphicsFormat.PDF, OutputSize.QrBillOnly, language, 0));

    /// <summary>Validates the request and renders the payment part with receipt as a PNG (for previews and visual checks).</summary>
    /// <exception cref="QrBillValidationException">The library reports an error or a warning for the data.</exception>
    public static byte[] RenderPng(QrBillRequest request, BillLanguage language = BillLanguage.French, int resolutionDpi = 200) =>
        QRBill.Generate(ValidatedBill(request, GraphicsFormat.PNG, OutputSize.QrBillOnly, language, resolutionDpi));

    /// <summary>Validates the request and renders the Swiss QR Code alone, with its quiet zone, as a PNG (the file the SIX validation portal accepts).</summary>
    /// <exception cref="QrBillValidationException">The library reports an error or a warning for the data.</exception>
    public static byte[] RenderQrCodePng(QrBillRequest request, int resolutionDpi = 300) =>
        QRBill.Generate(ValidatedBill(request, GraphicsFormat.PNG, OutputSize.QrCodeWithQuietZone, BillLanguage.French, resolutionDpi));

    /// <summary>Decodes the text of a Swiss QR Code back into a request (the reverse of <see cref="EncodeQrCodeText"/>).</summary>
    /// <exception cref="QrBillValidationException">The text is not a valid Swiss QR Code, or it breaks one of our rules (for example EUR).</exception>
    public static QrBillRequest DecodeQrCodeText(string qrCodeText)
    {
        Bill bill;
        try
        {
            bill = QRBill.DecodeQrCodeText(qrCodeText);
        }
        catch (QRBillValidationException e)
        {
            throw new QrBillValidationException("QR code text", Describe(e.Result));
        }

        PaymentReference reference = bill.ReferenceType switch
        {
            "QRR" => PaymentReference.QrReference(bill.Reference),
            "SCOR" => PaymentReference.CreditorReference(bill.Reference),
            _ => PaymentReference.None,
        };

        return new QrBillRequest(
            Iban.Parse(bill.Account),
            FromAddress(bill.Creditor),
            reference,
            bill.Amount,
            bill.Debtor is null ? null : FromAddress(bill.Debtor),
            bill.UnstructuredMessage,
            bill.Currency);
    }

    /// <summary>Maps the request to the library's model.</summary>
    internal static Bill ToBill(QrBillRequest request, GraphicsFormat graphicsFormat, OutputSize outputSize, BillLanguage language, int resolutionDpi)
    {
        var bill = new Bill
        {
            Account = request.Account.Value,
            Creditor = ToAddress(request.Creditor),
            Amount = request.Amount,
            Currency = request.Currency,
            Debtor = request.Debtor is null ? null : ToAddress(request.Debtor),
            Reference = request.Reference.Type == PaymentReferenceType.None ? null : request.Reference.Value,
            UnstructuredMessage = request.Message,
            CharacterSet = SpsCharacterSet.ExtendedLatin,
            Format = new BillFormat
            {
                GraphicsFormat = graphicsFormat,
                OutputSize = outputSize,
                Language = ToLanguage(language),
                SeparatorType = SeparatorType.DashedLineWithScissors,
            },
        };

        if (resolutionDpi > 0)
        {
            bill.Format.Resolution = resolutionDpi;
        }

        return bill;
    }

    private static Bill ValidatedBill(QrBillRequest request, GraphicsFormat graphicsFormat, OutputSize outputSize, BillLanguage language, int resolutionDpi)
    {
        ArgumentNullException.ThrowIfNull(request);
        Bill bill = ToBill(request, graphicsFormat, outputSize, language, resolutionDpi);
        ValidationResult result = QRBill.Validate(bill);

        // A warning means the library had to clean or clip a value; the output would differ from the input.
        if (!result.IsValid || result.HasWarnings)
        {
            throw new QrBillValidationException("QR-bill", Describe(result));
        }

        return bill;
    }

    private static string Describe(ValidationResult result) =>
        string.Join("; ", result.ValidationMessages.Select(m => $"{m.Field}: {m.MessageKey}"));

    private static Address ToAddress(StructuredAddress address) => new()
    {
        Name = address.Name,
        Street = address.Street,
        HouseNo = address.BuildingNumber,
        PostalCode = address.PostalCode,
        Town = address.Town,
        CountryCode = address.Country,
    };

    private static StructuredAddress FromAddress(Address address) =>
        new(address.Name, address.Street, address.HouseNo, address.PostalCode, address.Town, address.CountryCode);

    private static Language ToLanguage(BillLanguage language) => language switch
    {
        BillLanguage.German => Language.DE,
        BillLanguage.French => Language.FR,
        BillLanguage.Italian => Language.IT,
        BillLanguage.Romansh => Language.RM,
        BillLanguage.English => Language.EN,
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown language."),
    };
}
