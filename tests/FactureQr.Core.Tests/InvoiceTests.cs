using FactureQr.Core.Billing;
using FactureQr.Core.Invoicing;

namespace FactureQr.Core.Tests;

public class InvoiceTests
{
    // Debtor addresses from the examples in Annex A of the Implementation Guidelines.
    private static readonly StructuredAddress SimonMuster = new("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "CH");

    private static readonly DateOnly IssueDate = new(2026, 3, 2);
    private static readonly DateOnly DueDate = new(2026, 4, 1);

    [Fact]
    public void A_new_draft_is_empty_has_no_number_and_copies_the_customer_address()
    {
        var customer = new Customer(SimonMuster);

        var invoice = Invoice.CreateDraft(customer);

        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Equal(customer.Id, invoice.CustomerId);
        Assert.Equal(SimonMuster, invoice.BillTo);
        Assert.Empty(invoice.Lines);
        Assert.Equal(0m, invoice.Total);
        Assert.Null(invoice.Number);
        Assert.Null(invoice.IssueDate);
        Assert.Null(invoice.DueDate);
    }

    [Fact]
    public void The_line_amount_is_quantity_times_unit_price_rounded_half_away_from_zero()
    {
        var invoice = Draft();

        InvoiceLine line = invoice.AddLine("Consulting", 1.5m, 33.33m);

        Assert.Equal(50.00m, line.Amount); // 49.995 → 50.00
        Assert.Equal(1, line.Position);
    }

    [Fact]
    public void The_total_is_the_sum_of_the_lines_after_every_change()
    {
        var invoice = Draft();
        InvoiceLine first = invoice.AddLine("Analysis", 2m, 150m);
        invoice.AddLine("Development", 7.5m, 120m);
        invoice.AddLine("Travel", 1m, 48.60m);

        Assert.Equal(300m + 900m + 48.60m, invoice.Total);

        invoice.RemoveLine(first.Id);

        Assert.Equal(948.60m, invoice.Total);
        Assert.Equal(invoice.Lines.Sum(l => l.Amount), invoice.Total);
    }

    [Fact]
    public void The_total_equals_the_sum_of_the_lines_for_many_random_edits()
    {
        var random = new Random(20261112);
        var invoice = Draft();

        for (int i = 0; i < 500; i++)
        {
            if (invoice.Lines.Count > 0 && random.Next(3) == 0)
            {
                invoice.RemoveLine(invoice.Lines[random.Next(invoice.Lines.Count)].Id);
            }
            else
            {
                decimal quantity = random.Next(1, 100_000) / 1000m;
                decimal unitPrice = random.Next(0, 1_000_000) / 100m;
                invoice.AddLine($"Item {i}", quantity, unitPrice);
            }

            Assert.Equal(invoice.Lines.Sum(l => l.Amount), invoice.Total);
            Assert.Equal(Enumerable.Range(1, invoice.Lines.Count), invoice.Lines.Select(l => l.Position));
        }
    }

    [Fact]
    public void Removing_a_line_moves_the_following_lines_up()
    {
        var invoice = Draft();
        invoice.AddLine("A", 1m, 1m);
        InvoiceLine b = invoice.AddLine("B", 1m, 2m);
        invoice.AddLine("C", 1m, 3m);

        invoice.RemoveLine(b.Id);

        Assert.Equal(["A", "C"], invoice.Lines.Select(l => l.Description));
        Assert.Equal([1, 2], invoice.Lines.Select(l => l.Position));
    }

    [Fact]
    public void Removing_an_unknown_line_is_refused()
    {
        var invoice = Draft();

        Assert.Throws<InvoiceRuleException>(() => invoice.RemoveLine(Guid.NewGuid()));
    }

    [Fact]
    public void The_total_cannot_exceed_the_largest_QR_bill_amount_and_the_refused_line_is_not_added()
    {
        var invoice = Draft();
        invoice.AddLine("Almost everything", 1m, 999_999_999.00m);

        var e = Assert.Throws<InvoiceRuleException>(() => invoice.AddLine("One franc too many", 1m, 1m));

        Assert.Contains("999999999.99", e.Message, StringComparison.Ordinal);
        Assert.Single(invoice.Lines);
        Assert.Equal(999_999_999.00m, invoice.Total);

        invoice.AddLine("Exactly up to the limit", 1m, 0.99m);
        Assert.Equal(Invoice.MaxTotal, invoice.Total);
    }

    [Fact]
    public void A_single_line_cannot_exceed_the_largest_QR_bill_amount()
    {
        var invoice = Draft();

        Assert.Throws<InvoiceRuleException>(() => invoice.AddLine("Too much", 2m, 999_999_999.99m));
        Assert.Empty(invoice.Lines);
    }

    [Theory]
    [InlineData("", 1, 1)]
    [InlineData("   ", 1, 1)]
    [InlineData("Line\nbreak", 1, 1)]
    [InlineData("Tab\there", 1, 1)]
    [InlineData("Zero quantity", 0, 1)]
    [InlineData("Negative quantity", -1, 1)]
    [InlineData("Four decimals", 1.0001, 1)]
    [InlineData("Quantity too large", 1_000_000, 1)]
    [InlineData("Negative price", 1, -0.01)]
    [InlineData("Price with three decimals", 1, 0.001)]
    [InlineData("Price too large", 1, 1_000_000_000)]
    public void Invalid_lines_are_refused(string description, double quantity, double unitPrice)
    {
        var invoice = Draft();

        Assert.Throws<InvoiceRuleException>(() => invoice.AddLine(description, (decimal)quantity, (decimal)unitPrice));
        Assert.Empty(invoice.Lines);
        Assert.Equal(0m, invoice.Total);
    }

    [Fact]
    public void A_description_longer_than_200_characters_is_refused_and_one_of_200_is_accepted()
    {
        var invoice = Draft();

        Assert.Throws<InvoiceRuleException>(() => invoice.AddLine(new string('x', 201), 1m, 1m));
        Assert.Equal(200, invoice.AddLine(new string('x', 200), 1m, 1m).Description.Length);
    }

    [Fact]
    public void Issuing_sets_the_number_and_the_dates()
    {
        var invoice = Draft();
        invoice.AddLine("Consulting", 3m, 150m);

        invoice.Issue(new InvoiceNumber(2026, 7), IssueDate, DueDate);

        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal("2026-0007", invoice.Number?.ToString());
        Assert.Equal(IssueDate, invoice.IssueDate);
        Assert.Equal(DueDate, invoice.DueDate);
    }

    [Fact]
    public void An_invoice_without_lines_cannot_be_issued()
    {
        var invoice = Draft();

        var e = Assert.Throws<InvoiceRuleException>(() => invoice.Issue(new InvoiceNumber(2026, 1), IssueDate, DueDate));

        Assert.Contains("at least one line", e.Message, StringComparison.Ordinal);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
    }

    [Fact]
    public void An_invoice_with_a_total_below_the_smallest_QR_bill_amount_cannot_be_issued()
    {
        var invoice = Draft();
        invoice.AddLine("Free item", 1m, 0m);

        var e = Assert.Throws<InvoiceRuleException>(() => invoice.Issue(new InvoiceNumber(2026, 1), IssueDate, DueDate));

        Assert.Contains("0.01", e.Message, StringComparison.Ordinal);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Null(invoice.Number);
    }

    [Fact]
    public void The_number_must_belong_to_the_year_of_the_issue_date()
    {
        var invoice = DraftWithOneLine();

        Assert.Throws<InvoiceRuleException>(() => invoice.Issue(new InvoiceNumber(2025, 1), IssueDate, DueDate));
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
    }

    [Fact]
    public void The_due_date_cannot_be_before_the_issue_date()
    {
        var invoice = DraftWithOneLine();

        Assert.Throws<InvoiceRuleException>(() => invoice.Issue(new InvoiceNumber(2026, 1), IssueDate, IssueDate.AddDays(-1)));
        invoice.Issue(new InvoiceNumber(2026, 1), IssueDate, IssueDate);
        Assert.Equal(IssueDate, invoice.DueDate);
    }

    [Fact]
    public void An_issued_invoice_cannot_change_and_stays_as_it_was()
    {
        var customer = new Customer(SimonMuster);
        var invoice = Invoice.CreateDraft(customer);
        InvoiceLine line = invoice.AddLine("Consulting", 3m, 150m);
        invoice.Issue(new InvoiceNumber(2026, 1), IssueDate, DueDate);
        int revision = invoice.Revision;

        Assert.Throws<InvoiceRuleException>(() => invoice.AddLine("Extra", 1m, 10m));
        Assert.Throws<InvoiceRuleException>(() => invoice.RemoveLine(line.Id));
        Assert.Throws<InvoiceRuleException>(() => invoice.Issue(new InvoiceNumber(2026, 2), IssueDate, DueDate));

        Assert.Single(invoice.Lines);
        Assert.Equal(450m, invoice.Total);
        Assert.Equal(SimonMuster, invoice.BillTo);
        Assert.Equal("2026-0001", invoice.Number?.ToString());
        Assert.Equal(revision, invoice.Revision);
    }

    [Fact]
    public void Changing_the_customer_address_later_does_not_change_an_issued_invoice()
    {
        var customer = new Customer(SimonMuster);
        var invoice = Invoice.CreateDraft(customer);
        invoice.AddLine("Consulting", 1m, 100m);
        invoice.Issue(new InvoiceNumber(2026, 1), IssueDate, DueDate);

        customer.ChangeAddress(new StructuredAddress("Simon Muster", "Neue Strasse", "5", "8000", "Seldwyla", "CH"));

        Assert.Equal(SimonMuster, invoice.BillTo);
    }

    [Fact]
    public void Only_an_issued_invoice_can_be_marked_as_paid_and_a_paid_invoice_is_final()
    {
        var invoice = DraftWithOneLine();

        Assert.Throws<InvoiceRuleException>(() => invoice.MarkPaid(IssueDate));

        invoice.Issue(new InvoiceNumber(2026, 1), IssueDate, DueDate);
        invoice.MarkPaid(new DateOnly(2026, 3, 20));

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(new DateOnly(2026, 3, 20), invoice.PaidOn);
        Assert.Throws<InvoiceRuleException>(() => invoice.MarkPaid(new DateOnly(2026, 3, 21)));
        Assert.Throws<InvoiceRuleException>(() => invoice.AddLine("Extra", 1m, 1m));
    }

    [Fact]
    public void The_payment_date_cannot_be_before_the_issue_date()
    {
        var invoice = DraftWithOneLine();
        invoice.Issue(new InvoiceNumber(2026, 1), IssueDate, DueDate);

        Assert.Throws<InvoiceRuleException>(() => invoice.MarkPaid(IssueDate.AddDays(-1)));
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public void Overdue_is_derived_from_the_due_date_and_ends_with_payment()
    {
        var invoice = DraftWithOneLine();

        Assert.False(invoice.IsOverdue(DueDate.AddDays(10)));

        invoice.Issue(new InvoiceNumber(2026, 1), IssueDate, DueDate);

        Assert.False(invoice.IsOverdue(DueDate));
        Assert.True(invoice.IsOverdue(DueDate.AddDays(1)));

        invoice.MarkPaid(DueDate.AddDays(5));

        Assert.False(invoice.IsOverdue(DueDate.AddDays(10)));
    }

    private static Invoice Draft() => Invoice.CreateDraft(new Customer(SimonMuster));

    private static Invoice DraftWithOneLine()
    {
        var invoice = Draft();
        invoice.AddLine("Consulting", 1m, 100m);
        return invoice;
    }
}
