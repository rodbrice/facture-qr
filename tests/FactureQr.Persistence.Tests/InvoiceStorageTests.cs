using FactureQr.Core.Billing;
using FactureQr.Core.Invoicing;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Persistence.Tests;

[Collection(PostgreSqlGroup.Name)]
public class InvoiceStorageTests(PostgreSqlFixture postgres)
{
    private static readonly StructuredAddress SimonMuster = new("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "CH");
    private static readonly DateOnly IssueDate = new(2026, 3, 2);
    private static readonly DateOnly DueDate = new(2026, 4, 1);

    [Fact]
    public async Task A_draft_with_its_lines_and_address_is_read_back_as_it_was_saved()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        var customer = new Customer(SimonMuster);
        var draft = Invoice.CreateDraft(customer);
        draft.AddLine("Analysis", 2m, 150m);
        draft.AddLine("Development", 7.5m, 120m);
        draft.AddLine("Weighed material", 0.125m, 33.33m);
        await SaveAsync(cs, customer, draft);

        await using var db = Open(cs);
        Invoice stored = await db.Invoices.Include(i => i.Lines).SingleAsync(i => i.Id == draft.Id);

        Assert.Equal(InvoiceStatus.Draft, stored.Status);
        Assert.Equal(SimonMuster, stored.BillTo);
        Assert.Equal(customer.Id, stored.CustomerId);
        Assert.Equal(["Analysis", "Development", "Weighed material"], stored.Lines.Select(l => l.Description));
        Assert.Equal([2m, 7.5m, 0.125m], stored.Lines.Select(l => l.Quantity));
        Assert.Equal([300m, 900m, 4.17m], stored.Lines.Select(l => l.Amount));
        Assert.Equal(1204.17m, stored.Total);
        Assert.Null(stored.Number);
    }

    [Fact]
    public async Task Removing_a_line_of_a_stored_draft_renumbers_the_following_lines()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        var customer = new Customer(SimonMuster);
        var draft = Invoice.CreateDraft(customer);
        InvoiceLine first = draft.AddLine("A", 1m, 1m);
        draft.AddLine("B", 1m, 2m);
        draft.AddLine("C", 1m, 3m);
        draft.AddLine("D", 1m, 4m);
        await SaveAsync(cs, customer, draft);

        await using (var db = Open(cs))
        {
            Invoice stored = await db.Invoices.Include(i => i.Lines).SingleAsync(i => i.Id == draft.Id);
            stored.RemoveLine(first.Id);
            await db.SaveChangesAsync();
        }

        await using (var db = Open(cs))
        {
            Invoice stored = await db.Invoices.Include(i => i.Lines).SingleAsync(i => i.Id == draft.Id);
            Assert.Equal(["B", "C", "D"], stored.Lines.Select(l => l.Description));
            Assert.Equal([1, 2, 3], stored.Lines.Select(l => l.Position));
            Assert.Equal(9m, stored.Total);
        }
    }

    [Fact]
    public async Task Numbers_follow_each_other_within_a_year_and_restart_at_1_in_a_new_year()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        Guid a = await SaveDraftAsync(cs);
        Guid b = await SaveDraftAsync(cs);
        Guid c = await SaveDraftAsync(cs);

        Assert.Equal("2026-0001", (await IssueAsync(cs, a, new DateOnly(2026, 12, 30))).ToString());
        Assert.Equal("2026-0002", (await IssueAsync(cs, b, new DateOnly(2026, 12, 31))).ToString());
        Assert.Equal("2027-0001", (await IssueAsync(cs, c, new DateOnly(2027, 1, 1))).ToString());

        await using var db = Open(cs);
        Invoice stored = await db.Invoices.SingleAsync(i => i.Id == c);
        Assert.Equal(InvoiceStatus.Issued, stored.Status);
        Assert.Equal(new InvoiceNumber(2027, 1), stored.Number);
        Assert.Equal(new DateOnly(2027, 1, 1), stored.IssueDate);
    }

    [Fact]
    public async Task A_refused_issue_does_not_use_up_a_number()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        Guid draft = await SaveDraftAsync(cs);

        await Assert.ThrowsAsync<InvoiceRuleException>(() => IssueAsync(cs, draft, IssueDate, dueDate: IssueDate.AddDays(-1)));

        Assert.Equal(new InvoiceNumber(2026, 1), await IssueAsync(cs, draft, IssueDate));
    }

    [Fact]
    public async Task An_empty_draft_is_not_issued_and_does_not_use_up_a_number()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        var customer = new Customer(SimonMuster);
        var empty = Invoice.CreateDraft(customer);
        await SaveAsync(cs, customer, empty);
        Guid other = await SaveDraftAsync(cs);

        await Assert.ThrowsAsync<InvoiceRuleException>(() => IssueAsync(cs, empty.Id, IssueDate));

        Assert.Equal(new InvoiceNumber(2026, 1), await IssueAsync(cs, other, IssueDate));
    }

    [Fact]
    public async Task Issuing_an_unknown_or_already_issued_invoice_is_refused()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        Guid draft = await SaveDraftAsync(cs);
        await IssueAsync(cs, draft, IssueDate);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => IssueAsync(cs, Guid.NewGuid(), IssueDate));
        await Assert.ThrowsAsync<InvoiceRuleException>(() => IssueAsync(cs, draft, IssueDate));

        await using var db = Open(cs);
        Assert.Equal(1, await db.Invoices.CountAsync(i => i.NumberYear == 2026));
    }

    [Fact]
    public async Task A_stored_issued_invoice_refuses_changes_and_keeps_its_content()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        Guid id = await SaveDraftAsync(cs);
        await IssueAsync(cs, id, IssueDate);

        await using (var db = Open(cs))
        {
            Invoice stored = await db.Invoices.Include(i => i.Lines).SingleAsync(i => i.Id == id);
            Assert.Throws<InvoiceRuleException>(() => stored.AddLine("Extra", 1m, 10m));
            Assert.Throws<InvoiceRuleException>(() => stored.RemoveLine(stored.Lines[0].Id));
            Assert.Equal(0, await db.SaveChangesAsync());
        }

        await using (var db = Open(cs))
        {
            Invoice stored = await db.Invoices.Include(i => i.Lines).SingleAsync(i => i.Id == id);
            Assert.Single(stored.Lines);
            Assert.Equal(450m, stored.Total);
        }
    }

    [Fact]
    public async Task Marking_a_stored_invoice_as_paid_is_saved()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        Guid id = await SaveDraftAsync(cs);
        await IssueAsync(cs, id, IssueDate);

        await using (var db = Open(cs))
        {
            Invoice stored = await db.Invoices.SingleAsync(i => i.Id == id);
            stored.MarkPaid(new DateOnly(2026, 3, 20));
            await db.SaveChangesAsync();
        }

        await using (var db = Open(cs))
        {
            Invoice stored = await db.Invoices.SingleAsync(i => i.Id == id);
            Assert.Equal(InvoiceStatus.Paid, stored.Status);
            Assert.Equal(new DateOnly(2026, 3, 20), stored.PaidOn);
        }
    }

    [Fact]
    public async Task A_customer_address_change_is_saved_and_does_not_change_an_issued_invoice()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        var customer = new Customer(SimonMuster);
        var draft = Invoice.CreateDraft(customer);
        draft.AddLine("Consulting", 1m, 100m);
        await SaveAsync(cs, customer, draft);
        await IssueAsync(cs, draft.Id, IssueDate);
        var moved = new StructuredAddress("Simon Muster", "Neue Strasse", "5", "8000", "Seldwyla", "CH");

        await using (var db = Open(cs))
        {
            Customer stored = await db.Customers.SingleAsync(c => c.Id == customer.Id);
            stored.ChangeAddress(moved);
            await db.SaveChangesAsync();
        }

        await using (var db = Open(cs))
        {
            Assert.Equal(moved, (await db.Customers.SingleAsync(c => c.Id == customer.Id)).Address);
            Assert.Equal(SimonMuster, (await db.Invoices.SingleAsync(i => i.Id == draft.Id)).BillTo);
        }
    }

    private static FactureQrDbContext Open(string cs) => new(FactureQrDbContext.CreateOptions(cs));

    private static async Task SaveAsync(string cs, Customer customer, Invoice invoice)
    {
        await using var db = Open(cs);
        db.Customers.Add(customer);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> SaveDraftAsync(string cs)
    {
        var customer = new Customer(SimonMuster);
        var draft = Invoice.CreateDraft(customer);
        draft.AddLine("Consulting", 3m, 150m);
        await SaveAsync(cs, customer, draft);
        return draft.Id;
    }

    private static async Task<InvoiceNumber> IssueAsync(string cs, Guid id, DateOnly issueDate, DateOnly? dueDate = null)
    {
        await using var db = Open(cs);
        return await new InvoiceIssuer(db).IssueAsync(id, issueDate, dueDate ?? issueDate.AddDays(30));
    }
}
