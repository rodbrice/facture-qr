namespace FactureQr.Core.Billing;

/// <summary>
/// A Swiss or Liechtenstein IBAN (ISO 13616) as accepted in a QR-bill:
/// 21 characters, country code CH or LI, valid modulo-97 check digits.
/// The Swiss account part is numeric; the Liechtenstein account part
/// (positions 10 to 21) may contain letters, as in the IBAN registry.
/// A QR-IBAN is an IBAN whose institution identifier (positions 5 to 9)
/// lies in the QR-IID range 30000–31999; it may only be used with a QR reference.
/// </summary>
public sealed record Iban
{
    /// <summary>Length of a Swiss or Liechtenstein IBAN.</summary>
    public const int Length = 21;

    private const int QrIidMin = 30000;
    private const int QrIidMax = 31999;

    private Iban(string value, bool isQrIban)
    {
        Value = value;
        IsQrIban = isQrIban;
    }

    /// <summary>The IBAN in compact form: upper case, no spaces.</summary>
    public string Value { get; }

    /// <summary>True when the institution identifier is in the QR-IID range 30000–31999.</summary>
    public bool IsQrIban { get; }

    /// <summary>The IBAN printed in groups of four characters, as on the payment part.</summary>
    public string Formatted => string.Join(' ', Enumerable.Range(0, (Value.Length + 3) / 4).Select(i => Value.Substring(i * 4, Math.Min(4, Value.Length - (i * 4)))));

    /// <summary>Parses an IBAN; spaces are ignored and letters are upper-cased.</summary>
    /// <exception cref="QrBillValidationException">The value is not a valid CH/LI IBAN.</exception>
    public static Iban Parse(string text)
    {
        const string field = "IBAN";
        string value = string.Concat((text ?? string.Empty).Where(c => c != ' ')).ToUpperInvariant();

        if (value.Length != Length)
        {
            throw new QrBillValidationException(field, $"a Swiss or Liechtenstein IBAN has {Length} characters (got {value.Length}).");
        }

        if (!value.StartsWith("CH", StringComparison.Ordinal) && !value.StartsWith("LI", StringComparison.Ordinal))
        {
            throw new QrBillValidationException(field, "only IBANs with the country code CH or LI are permitted in a QR-bill.");
        }

        if (!value.All(char.IsAsciiLetterOrDigit))
        {
            throw new QrBillValidationException(field, "only letters and digits are permitted.");
        }

        if (!value[2..9].All(char.IsAsciiDigit))
        {
            throw new QrBillValidationException(field, "positions 3 to 9 (check digits and institution identifier) must be digits.");
        }

        if (value.StartsWith("CH", StringComparison.Ordinal) && !value.Skip(9).All(char.IsAsciiDigit))
        {
            throw new QrBillValidationException(field, "positions 10 to 21 of a Swiss IBAN must be digits.");
        }

        if (Modulo97.Remainder(value[4..] + value[..4]) != 1)
        {
            throw new QrBillValidationException(field, "the check digits are wrong.");
        }

        int iid = int.Parse(value.AsSpan(4, 5), System.Globalization.CultureInfo.InvariantCulture);
        return new Iban(value, iid is >= QrIidMin and <= QrIidMax);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
