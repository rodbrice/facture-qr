using FactureQr.Core.Invoicing;

namespace FactureQr.Core.Tests;

public class InvoiceNumberTests
{
    [Theory]
    [InlineData(2026, 1, "2026-0001")]
    [InlineData(2026, 42, "2026-0042")]
    [InlineData(2027, 9999, "2027-9999")]
    [InlineData(2027, 12345, "2027-12345")]
    public void The_printed_form_is_the_year_and_a_sequence_of_at_least_four_digits(int year, int sequence, string expected)
    {
        Assert.Equal(expected, new InvoiceNumber(year, sequence).ToString());
    }

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(10000, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, -1)]
    public void Years_outside_2000_to_9999_and_sequences_below_1_are_refused(int year, int sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvoiceNumber(year, sequence));
    }

    [Fact]
    public void Two_numbers_with_the_same_year_and_sequence_are_equal()
    {
        Assert.Equal(new InvoiceNumber(2026, 3), new InvoiceNumber(2026, 3));
        Assert.NotEqual(new InvoiceNumber(2026, 3), new InvoiceNumber(2027, 3));
    }
}
