using System.Globalization;

namespace FactureQr.Core.Invoicing;

/// <summary>
/// An invoice number: the year of the issue date and a sequence that starts
/// at 1 every year, printed as <c>2026-0001</c>. Numbers are assigned by the
/// persistence layer when an invoice is issued (see ADR-0003).
/// </summary>
public sealed record InvoiceNumber
{
    /// <summary>Earliest year accepted.</summary>
    public const int MinYear = 2000;

    /// <summary>Latest year accepted.</summary>
    public const int MaxYear = 9999;

    /// <summary>Creates a number for the given year and sequence.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The year is outside 2000–9999 or the sequence is not positive.</exception>
    public InvoiceNumber(int year, int sequence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, MinYear);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, MaxYear);
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1);

        Year = year;
        Sequence = sequence;
    }

    /// <summary>The year of the issue date.</summary>
    public int Year { get; }

    /// <summary>The position of the invoice in its year, starting at 1.</summary>
    public int Sequence { get; }

    /// <summary>The printed form: year, hyphen, sequence padded to at least four digits.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Year}-{Sequence:D4}");
}
