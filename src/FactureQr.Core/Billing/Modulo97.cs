namespace FactureQr.Core.Billing;

/// <summary>
/// Modulo 97-10 arithmetic (ISO 7064) shared by the IBAN check (ISO 13616)
/// and the Creditor Reference check (ISO 11649).
/// </summary>
internal static class Modulo97
{
    /// <summary>
    /// Computes the remainder modulo 97 of an alphanumeric string, after
    /// replacing each letter by its number (A = 10 … Z = 35). The digits are
    /// consumed one at a time so the intermediate value never overflows.
    /// </summary>
    public static int Remainder(string alphanumeric)
    {
        int remainder = 0;
        foreach (char c in alphanumeric)
        {
            int number = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'A' and <= 'Z' => c - 'A' + 10,
                >= 'a' and <= 'z' => c - 'a' + 10,
                _ => throw new ArgumentException($"'{c}' is not alphanumeric.", nameof(alphanumeric)),
            };

            // A letter contributes two digits (10–35), a digit one.
            remainder = number >= 10
                ? ((remainder * 100) + number) % 97
                : ((remainder * 10) + number) % 97;
        }

        return remainder;
    }
}
