namespace FactureQr.Core.Invoicing;

/// <summary>
/// One line of an invoice: a description, a quantity and a unit price in CHF.
/// The amount is the quantity times the unit price, rounded to the centime
/// (half away from zero). Lines are created and removed only through
/// <see cref="Invoice"/>, which keeps the total equal to the sum of the lines.
/// </summary>
public sealed class InvoiceLine
{
    /// <summary>Maximum length of the description.</summary>
    public const int MaxDescriptionLength = 200;

    /// <summary>Largest quantity accepted.</summary>
    public const decimal MaxQuantity = 999_999.999m;

    /// <summary>Number of decimals accepted in a quantity (for example 1.5 hours or 0.125 kg).</summary>
    public const int QuantityDecimals = 3;

    /// <summary>Number of decimals accepted in a unit price and an amount (centimes).</summary>
    public const int AmountDecimals = 2;

    internal InvoiceLine(int position, string description, decimal quantity, decimal unitPrice)
    {
        Id = Guid.CreateVersion7();
        Position = position;
        Description = ValidateDescription(description);
        Quantity = ValidateQuantity(quantity);
        UnitPrice = ValidateUnitPrice(unitPrice);
        Amount = ComputeAmount(Quantity, UnitPrice);
    }

    // Used by the persistence layer to materialise a stored line.
    private InvoiceLine()
    {
        Description = null!;
    }

    /// <summary>The identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>The position of the line on the invoice, starting at 1.</summary>
    public int Position { get; internal set; }

    /// <summary>What is invoiced.</summary>
    public string Description { get; private set; }

    /// <summary>The quantity, greater than zero, with at most three decimals.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>The price of one unit in CHF, zero or more, with at most two decimals.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Quantity × unit price, rounded to the centime.</summary>
    public decimal Amount { get; private set; }

    private static string ValidateDescription(string? description)
    {
        string value = (description ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            throw new InvoiceRuleException("A line needs a description.");
        }

        if (value.Length > MaxDescriptionLength)
        {
            throw new InvoiceRuleException($"A line description has at most {MaxDescriptionLength} characters (got {value.Length}).");
        }

        if (value.Any(char.IsControl))
        {
            throw new InvoiceRuleException("A line description cannot contain control characters such as line breaks or tabs.");
        }

        return value;
    }

    private static decimal ValidateQuantity(decimal quantity)
    {
        if (quantity is <= 0 or > MaxQuantity)
        {
            throw new InvoiceRuleException($"The quantity must be greater than 0 and at most {MaxQuantity} (got {quantity}).");
        }

        if (decimal.Round(quantity, QuantityDecimals) != quantity)
        {
            throw new InvoiceRuleException($"The quantity has at most {QuantityDecimals} decimals (got {quantity}).");
        }

        return quantity;
    }

    private static decimal ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice is < 0 or > Invoice.MaxTotal)
        {
            throw new InvoiceRuleException($"The unit price must be between 0 and {Invoice.MaxTotal} CHF (got {unitPrice}).");
        }

        if (decimal.Round(unitPrice, AmountDecimals) != unitPrice)
        {
            throw new InvoiceRuleException($"The unit price has at most {AmountDecimals} decimals (got {unitPrice}).");
        }

        return unitPrice;
    }

    private static decimal ComputeAmount(decimal quantity, decimal unitPrice)
    {
        decimal amount = decimal.Round(quantity * unitPrice, AmountDecimals, MidpointRounding.AwayFromZero);
        if (amount > Invoice.MaxTotal)
        {
            throw new InvoiceRuleException($"The line amount {amount} CHF exceeds the largest amount a QR-bill can carry ({Invoice.MaxTotal} CHF).");
        }

        return amount;
    }
}
