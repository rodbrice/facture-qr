namespace FactureQr.Core.Billing;

/// <summary>
/// Raised when a value does not satisfy the rules of the Swiss QR-bill
/// (Implementation Guidelines v2.3/v2.4). The message names the field and
/// explains the rule that was broken.
/// </summary>
public sealed class QrBillValidationException : Exception
{
    /// <summary>Creates an exception for the given field and rule.</summary>
    public QrBillValidationException(string field, string message)
        : base($"{field}: {message}")
    {
        Field = field;
    }

    /// <summary>Creates an exception without field information.</summary>
    public QrBillValidationException()
    {
        Field = string.Empty;
    }

    /// <summary>Creates an exception with a message only.</summary>
    public QrBillValidationException(string message)
        : base(message)
    {
        Field = string.Empty;
    }

    /// <summary>Creates an exception with a message and an inner exception.</summary>
    public QrBillValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Field = string.Empty;
    }

    /// <summary>Name of the field that failed validation, or an empty string.</summary>
    public string Field { get; }
}
