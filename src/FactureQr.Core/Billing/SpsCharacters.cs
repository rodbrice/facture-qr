namespace FactureQr.Core.Billing;

/// <summary>
/// Character set permitted in the Swiss QR Code
/// (Implementation Guidelines v2.4, chapter 4.1.1, "Extended Latin").
/// </summary>
public static class SpsCharacters
{
    /// <summary>Returns true when the character may appear in a Swiss QR Code.</summary>
    public static bool IsAllowed(char c) =>
        c is (>= ' ' and <= '~')   // Basic Latin
            or (>= ' ' and <= 'ÿ') // Latin-1 Supplement
            or (>= 'Ā' and <= 'ſ') // Latin Extended-A
            or (>= 'Ș' and <= 'ț') // Ș ș Ț ț
            or '€';                     // €

    /// <summary>Returns true when every character of the text is allowed.</summary>
    public static bool IsAllowed(string text) => text.All(IsAllowed);

    /// <summary>
    /// Validates a text field: trims it, checks its length and its characters.
    /// Returns the trimmed text, or null when the field is optional and empty.
    /// </summary>
    internal static string? Validate(string field, string? text, int maxLength, bool required)
    {
        string? trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return required
                ? throw new QrBillValidationException(field, "the value is required.")
                : null;
        }

        if (trimmed.Length > maxLength)
        {
            throw new QrBillValidationException(field, $"at most {maxLength} characters are permitted (got {trimmed.Length}).");
        }

        int bad = trimmed.ToList().FindIndex(c => !IsAllowed(c));
        if (bad >= 0)
        {
            throw new QrBillValidationException(field, $"the character '{trimmed[bad]}' (U+{(int)trimmed[bad]:X4}) is outside the permitted character set.");
        }

        return trimmed;
    }
}
