namespace FactureQr.Core.Billing;

/// <summary>The three reference variants of a QR-bill (Implementation Guidelines v2.4, chapter 2.12 and 4.3.2).</summary>
public enum PaymentReferenceType
{
    /// <summary>No structured reference (code NON). Only with a plain IBAN.</summary>
    None,

    /// <summary>QR reference (code QRR): 27 digits with a modulo 10 recursive check digit. Only with a QR-IBAN and only in CHF.</summary>
    QrReference,

    /// <summary>Creditor Reference ISO 11649 (code SCOR): RF + 2 check digits + up to 21 alphanumeric characters. Only with a plain IBAN.</summary>
    CreditorReference,
}

/// <summary>
/// The structured payment reference of a QR-bill: a QR reference, a
/// Creditor Reference (ISO 11649) or none. Values are validated on creation.
/// </summary>
public sealed record PaymentReference
{
    /// <summary>Length of a QR reference, check digit included.</summary>
    public const int QrReferenceLength = 27;

    /// <summary>Minimum length of a Creditor Reference, "RF" and check digits included.</summary>
    public const int CreditorReferenceMinLength = 5;

    /// <summary>Maximum length of a Creditor Reference, "RF" and check digits included.</summary>
    public const int CreditorReferenceMaxLength = 25;

    // Modulo 10 recursive: the carry-over table from Annex B of the Implementation Guidelines.
    private static readonly int[] Modulo10Table = [0, 9, 4, 6, 8, 2, 7, 1, 3, 5];

    private PaymentReference(PaymentReferenceType type, string value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>The "no reference" variant (code NON).</summary>
    public static PaymentReference None { get; } = new(PaymentReferenceType.None, string.Empty);

    /// <summary>The reference variant.</summary>
    public PaymentReferenceType Type { get; }

    /// <summary>The reference in compact form (no spaces, upper case), or an empty string for <see cref="PaymentReferenceType.None"/>.</summary>
    public string Value { get; }

    /// <summary>The code written in the QR code: QRR, SCOR or NON.</summary>
    public string TypeCode => Type switch
    {
        PaymentReferenceType.QrReference => "QRR",
        PaymentReferenceType.CreditorReference => "SCOR",
        _ => "NON",
    };

    /// <summary>
    /// The reference as printed on the payment part: a QR reference in groups
    /// of five digits from the right, a Creditor Reference in groups of four
    /// characters from the left.
    /// </summary>
    public string Formatted => Type switch
    {
        PaymentReferenceType.QrReference => Group(Value, 5, fromRight: true),
        PaymentReferenceType.CreditorReference => Group(Value, 4, fromRight: false),
        _ => string.Empty,
    };

    /// <summary>Accepts an existing 27-digit QR reference and verifies its check digit.</summary>
    /// <exception cref="QrBillValidationException">The reference is not 27 digits, is all zeros or has a wrong check digit.</exception>
    public static PaymentReference QrReference(string reference)
    {
        const string field = "QR reference";
        string value = Compact(reference);

        if (value.Length != QrReferenceLength)
        {
            throw new QrBillValidationException(field, $"a QR reference has exactly {QrReferenceLength} digits (got {value.Length}).");
        }

        if (!value.All(char.IsAsciiDigit))
        {
            throw new QrBillValidationException(field, "only digits are permitted.");
        }

        if (value.All(c => c == '0'))
        {
            throw new QrBillValidationException(field, "the reference must not consist exclusively of zeros.");
        }

        int expected = Modulo10RecursiveCheckDigit(value[..^1]);
        if (value[^1] - '0' != expected)
        {
            throw new QrBillValidationException(field, $"the check digit is wrong (expected {expected}).");
        }

        return new PaymentReference(PaymentReferenceType.QrReference, value);
    }

    /// <summary>
    /// Builds a QR reference from up to 26 digits: the digits are left-padded
    /// with zeros to 26 positions and the modulo 10 recursive check digit is appended.
    /// </summary>
    /// <exception cref="QrBillValidationException">The input is empty, longer than 26 digits, not numeric or all zeros.</exception>
    public static PaymentReference CreateQrReference(string digits)
    {
        const string field = "QR reference";
        string value = Compact(digits);

        if (value.Length is 0 or > QrReferenceLength - 1)
        {
            throw new QrBillValidationException(field, $"between 1 and {QrReferenceLength - 1} digits are required to build a QR reference (got {value.Length}).");
        }

        if (!value.All(char.IsAsciiDigit))
        {
            throw new QrBillValidationException(field, "only digits are permitted.");
        }

        string body = value.PadLeft(QrReferenceLength - 1, '0');
        return QrReference(body + Modulo10RecursiveCheckDigit(body));
    }

    /// <summary>Accepts an existing Creditor Reference (ISO 11649) and verifies its check digits.</summary>
    /// <exception cref="QrBillValidationException">The reference does not start with RF, has a wrong length, contains other characters than letters and digits, or has wrong check digits.</exception>
    public static PaymentReference CreditorReference(string reference)
    {
        const string field = "Creditor Reference";
        string value = Compact(reference);

        if (value.Length is < CreditorReferenceMinLength or > CreditorReferenceMaxLength)
        {
            throw new QrBillValidationException(field, $"a Creditor Reference has between {CreditorReferenceMinLength} and {CreditorReferenceMaxLength} characters (got {value.Length}).");
        }

        if (!value.StartsWith("RF", StringComparison.Ordinal))
        {
            throw new QrBillValidationException(field, "a Creditor Reference starts with RF.");
        }

        if (!value.All(char.IsAsciiLetterOrDigit))
        {
            throw new QrBillValidationException(field, "only letters and digits are permitted.");
        }

        if (!value[2..4].All(char.IsAsciiDigit))
        {
            throw new QrBillValidationException(field, "positions 3 and 4 are the check digits and must be numeric.");
        }

        // ISO 11649: move "RF" and the check digits to the end; the whole must leave remainder 1 modulo 97.
        if (Modulo97.Remainder(value[4..] + value[..4]) != 1)
        {
            throw new QrBillValidationException(field, $"the check digits are wrong (expected {Iso11649CheckDigits(value[4..]):D2}).");
        }

        return new PaymentReference(PaymentReferenceType.CreditorReference, value);
    }

    /// <summary>
    /// Builds a Creditor Reference (ISO 11649) from 1 to 21 letters or digits
    /// by prefixing "RF" and the two modulo 97-10 check digits.
    /// </summary>
    /// <exception cref="QrBillValidationException">The body is empty, longer than 21 characters or not alphanumeric.</exception>
    public static PaymentReference CreateCreditorReference(string body)
    {
        const string field = "Creditor Reference";
        string value = Compact(body);

        if (value.Length is 0 or > CreditorReferenceMaxLength - 4)
        {
            throw new QrBillValidationException(field, $"between 1 and {CreditorReferenceMaxLength - 4} letters or digits are required to build a Creditor Reference (got {value.Length}).");
        }

        if (!value.All(char.IsAsciiLetterOrDigit))
        {
            throw new QrBillValidationException(field, "only letters and digits are permitted.");
        }

        return CreditorReference($"RF{Iso11649CheckDigits(value):D2}{value}");
    }

    /// <summary>
    /// Computes the check digit of a QR reference with the "modulo 10 recursive"
    /// algorithm of Annex B of the Implementation Guidelines: starting with a
    /// carry-over of 0, each digit is added to the carry-over, reduced modulo 10
    /// and looked up in the table; the check digit is (10 - carry) mod 10.
    /// </summary>
    /// <param name="digits">The 26 digits preceding the check digit (any number of digits is accepted).</param>
    public static int Modulo10RecursiveCheckDigit(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        int carry = 0;
        foreach (char c in digits)
        {
            if (!char.IsAsciiDigit(c))
            {
                throw new ArgumentException("Only digits are permitted.", nameof(digits));
            }

            carry = Modulo10Table[(carry + (c - '0')) % 10];
        }

        return (10 - carry) % 10;
    }

    /// <summary>
    /// Computes the two check digits of a Creditor Reference (ISO 11649) for
    /// the given alphanumeric body: 98 minus the remainder modulo 97 of
    /// "body + RF00" with letters replaced by their numbers (A = 10 … Z = 35).
    /// </summary>
    public static int Iso11649CheckDigits(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return 98 - Modulo97.Remainder(body + "RF00");
    }

    /// <inheritdoc />
    public override string ToString() => Type == PaymentReferenceType.None ? "NON" : $"{TypeCode} {Value}";

    private static string Compact(string? text) =>
        string.Concat((text ?? string.Empty).Where(c => c != ' ')).ToUpperInvariant();

    private static string Group(string value, int size, bool fromRight)
    {
        var groups = new List<string>();
        if (fromRight)
        {
            int first = value.Length % size;
            if (first > 0)
            {
                groups.Add(value[..first]);
            }

            for (int i = first; i < value.Length; i += size)
            {
                groups.Add(value.Substring(i, size));
            }
        }
        else
        {
            for (int i = 0; i < value.Length; i += size)
            {
                groups.Add(value.Substring(i, Math.Min(size, value.Length - i)));
            }
        }

        return string.Join(' ', groups);
    }
}
