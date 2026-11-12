using FactureQr.Core.Billing;

namespace FactureQr.Core.Samples;

/// <summary>
/// Three synthetic QR-bills, one per reference variant. Every account, name
/// and address comes from the examples in Annex A of the SIX Implementation
/// Guidelines v2.4 and is fictitious by construction. The same data feeds the
/// golden tests and the files under docs/samples.
/// </summary>
public static class SyntheticSamples
{
    private static readonly StructuredAddress MaxMuster = new("Max Muster & Söhne", "Musterstrasse", "123", "8000", "Seldwyla", "CH");
    private static readonly StructuredAddress SimonMuster = new("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "CH");
    private static readonly StructuredAddress SarahBeispiel = new("Sarah Beispiel", "Musterstrasse", "1", "8000", "Seldwyla", "CH");
    private static readonly StructuredAddress MusterStiftung = new("Muster Stiftung", null, null, "3001", "Bern", "CH");

    /// <summary>Sample A: QR-IBAN with a QR reference (QRR), amount, debtor and message.</summary>
    public static QrBillRequest A { get; } = new(
        Iban.Parse("CH44 3199 9123 0008 8901 2"),
        MaxMuster,
        PaymentReference.QrReference("21 00000 00003 13947 14300 09017"),
        amount: 1949.75m,
        debtor: SimonMuster,
        message: "Invoice 2026-001");

    /// <summary>Sample B: plain IBAN with a Creditor Reference (SCOR), amount, debtor and message.</summary>
    public static QrBillRequest B { get; } = new(
        Iban.Parse("CH58 0079 1123 0008 8901 2"),
        MaxMuster,
        PaymentReference.CreditorReference("RF18 5390 0754 7034"),
        amount: 199.95m,
        debtor: SarahBeispiel,
        message: "Invoice 2026-002");

    /// <summary>Sample C: plain IBAN without reference (NON), amount, debtor and message.</summary>
    public static QrBillRequest C { get; } = new(
        Iban.Parse("CH52 0483 5012 3456 7100 0"),
        MusterStiftung,
        PaymentReference.None,
        amount: 120.00m,
        debtor: SimonMuster,
        message: "Invoice 2026-003");

    /// <summary>All samples with their identifier (a, b, c).</summary>
    public static IReadOnlyList<(string Id, QrBillRequest Request)> All { get; } = [("a", A), ("b", B), ("c", C)];
}
