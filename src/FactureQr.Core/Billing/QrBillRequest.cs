namespace FactureQr.Core.Billing;

/// <summary>
/// Everything needed to produce one QR-bill payment part in CHF: the creditor
/// account and address, an optional amount, an optional debtor, the payment
/// reference and an optional message. The constructor enforces the rules that
/// link these fields together (Implementation Guidelines v2.4, chapter 4):
/// a QR-IBAN requires a QR reference, a plain IBAN excludes it, and only CHF
/// is supported in this version (see ADR-0001).
/// </summary>
public sealed record QrBillRequest
{
    /// <summary>The only currency supported in this version.</summary>
    public const string SupportedCurrency = "CHF";

    /// <summary>Smallest permitted amount.</summary>
    public const decimal MinAmount = 0.01m;

    /// <summary>Largest permitted amount.</summary>
    public const decimal MaxAmount = 999_999_999.99m;

    /// <summary>Maximum length of the unstructured message.</summary>
    public const int MaxMessageLength = 140;

    /// <summary>Creates and validates a request.</summary>
    /// <param name="account">The creditor's IBAN or QR-IBAN.</param>
    /// <param name="creditor">The creditor's structured address (must match the account holder).</param>
    /// <param name="reference">The payment reference; its type must match the account kind.</param>
    /// <param name="amount">The amount in CHF with at most two decimals, or null when the payer fills it in.</param>
    /// <param name="debtor">The debtor's structured address, or null.</param>
    /// <param name="message">An unstructured message of at most 140 characters, or null.</param>
    /// <param name="currency">The currency; only CHF is accepted.</param>
    /// <exception cref="QrBillValidationException">A rule is violated; the message names the field and the rule.</exception>
    public QrBillRequest(
        Iban account,
        StructuredAddress creditor,
        PaymentReference reference,
        decimal? amount = null,
        StructuredAddress? debtor = null,
        string? message = null,
        string currency = SupportedCurrency)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(creditor);
        ArgumentNullException.ThrowIfNull(reference);

        Account = account;
        Creditor = creditor;
        Reference = ValidateReference(account, reference);
        Currency = ValidateCurrency(currency);
        Amount = ValidateAmount(amount);
        Debtor = debtor;
        Message = SpsCharacters.Validate("Message", message, MaxMessageLength, required: false);
    }

    /// <summary>The creditor's IBAN or QR-IBAN.</summary>
    public Iban Account { get; }

    /// <summary>The creditor's structured address.</summary>
    public StructuredAddress Creditor { get; }

    /// <summary>The payment reference (QRR, SCOR or none).</summary>
    public PaymentReference Reference { get; }

    /// <summary>The currency code; always CHF in this version.</summary>
    public string Currency { get; }

    /// <summary>The amount, or null when the payer fills it in.</summary>
    public decimal? Amount { get; }

    /// <summary>The debtor's structured address, or null.</summary>
    public StructuredAddress? Debtor { get; }

    /// <summary>The unstructured message, or null.</summary>
    public string? Message { get; }

    private static PaymentReference ValidateReference(Iban account, PaymentReference reference)
    {
        const string field = "Reference";
        if (account.IsQrIban && reference.Type != PaymentReferenceType.QrReference)
        {
            throw new QrBillValidationException(field, $"the account {account.Formatted} is a QR-IBAN and requires a QR reference (QRR); a Creditor Reference or no reference needs a plain IBAN.");
        }

        if (!account.IsQrIban && reference.Type == PaymentReferenceType.QrReference)
        {
            throw new QrBillValidationException(field, $"the account {account.Formatted} is a plain IBAN and cannot carry a QR reference (QRR); use a Creditor Reference (SCOR), no reference, or a QR-IBAN.");
        }

        return reference;
    }

    private static string ValidateCurrency(string? currency)
    {
        string value = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (value != SupportedCurrency)
        {
            throw new QrBillValidationException("Currency", $"only {SupportedCurrency} is supported in this version (got '{value}'); invoicing in EUR is out of scope, see ADR-0001.");
        }

        return value;
    }

    private static decimal? ValidateAmount(decimal? amount)
    {
        const string field = "Amount";
        if (amount is null)
        {
            return null;
        }

        decimal value = amount.Value;
        if (value is < MinAmount or > MaxAmount)
        {
            throw new QrBillValidationException(field, $"the amount must be between {MinAmount} and {MaxAmount} (got {value}).");
        }

        if (decimal.Round(value, 2) != value)
        {
            throw new QrBillValidationException(field, $"at most two decimals are permitted (got {value}).");
        }

        return decimal.Round(value, 2);
    }
}
