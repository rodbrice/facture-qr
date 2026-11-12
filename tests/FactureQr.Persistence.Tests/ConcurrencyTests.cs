using FactureQr.Core.Billing;
using FactureQr.Core.Invoicing;
using Microsoft.EntityFrameworkCore;

namespace FactureQr.Persistence.Tests;

[Collection(PostgreSqlGroup.Name)]
public class ConcurrencyTests(PostgreSqlFixture postgres)
{
    private const int Parallel = 40;

    private static readonly StructuredAddress SimonMuster = new("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "CH");
    private static readonly DateOnly IssueDate = new(2026, 3, 2);

    [Fact]
    public async Task Drafts_issued_at_the_same_time_get_distinct_numbers_without_gaps()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        List<Guid> drafts = await SaveDraftsAsync(cs, Parallel);

        // Each issue runs on its own open connection; the start signal releases them together.
        InvoiceNumber[] numbers = await RunTogetherAsync(cs, drafts, (db, id) => new InvoiceIssuer(db).IssueAsync(id, IssueDate, IssueDate.AddDays(30)));

        Assert.Equal(Enumerable.Range(1, Parallel), numbers.Select(n => n.Sequence).Order());
        Assert.All(numbers, n => Assert.Equal(2026, n.Year));

        await using var check = Open(cs);
        List<int> stored = await check.Invoices.Where(i => i.NumberYear == 2026).Select(i => i.NumberSequence!.Value).OrderBy(s => s).ToListAsync();
        Assert.Equal(Enumerable.Range(1, Parallel), stored);
    }

    [Fact]
    public async Task The_first_invoices_of_a_new_year_issued_at_the_same_time_get_distinct_numbers()
    {
        // No counter row exists for 2027 yet: every transaction tries to create it.
        string cs = await postgres.CreateMigratedDatabaseAsync();
        List<Guid> drafts = await SaveDraftsAsync(cs, Parallel);
        var newYear = new DateOnly(2027, 1, 1);

        InvoiceNumber[] numbers = await RunTogetherAsync(cs, drafts, (db, id) => new InvoiceIssuer(db).IssueAsync(id, newYear, newYear.AddDays(30)));

        Assert.Equal(Enumerable.Range(1, Parallel), numbers.Select(n => n.Sequence).Order());
    }

    [Fact]
    public async Task The_same_draft_issued_twice_at_the_same_time_is_issued_once_and_uses_one_number()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        Guid draft = (await SaveDraftsAsync(cs, 1))[0];

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<InvoiceNumber>[] attempts = [.. Enumerable.Range(0, 2).Select(_ => StartWhenSignalledAsync(cs, start.Task, db => new InvoiceIssuer(db).IssueAsync(draft, IssueDate, IssueDate.AddDays(30))))];
        start.SetResult();

        try
        {
            await Task.WhenAll(attempts);
        }
        catch (Exception)
        {
            // Inspected below.
        }

        Task<InvoiceNumber> succeeded = Assert.Single(attempts, t => t.IsCompletedSuccessfully);
        Task<InvoiceNumber> failed = Assert.Single(attempts, t => t.IsFaulted);
        Assert.Equal(new InvoiceNumber(2026, 1), await succeeded);
        Assert.True(
            failed.Exception!.InnerException is DbUpdateConcurrencyException or InvoiceRuleException,
            $"Unexpected failure: {failed.Exception.InnerException}");

        // The failed attempt rolled back its increment: the next invoice gets number 2.
        Guid next = (await SaveDraftsAsync(cs, 1))[0];
        await using var db = Open(cs);
        Assert.Equal(new InvoiceNumber(2026, 2), await new InvoiceIssuer(db).IssueAsync(next, IssueDate, IssueDate.AddDays(30)));
    }

    [Fact]
    public async Task A_draft_edited_from_a_stale_copy_after_it_was_issued_is_not_changed()
    {
        string cs = await postgres.CreateMigratedDatabaseAsync();
        Guid id = (await SaveDraftsAsync(cs, 1))[0];

        // A user loads the draft, then the same draft is issued elsewhere.
        await using var stale = Open(cs);
        Invoice staleCopy = await stale.Invoices.Include(i => i.Lines).SingleAsync(i => i.Id == id);
        await using (var other = Open(cs))
        {
            await new InvoiceIssuer(other).IssueAsync(id, IssueDate, IssueDate.AddDays(30));
        }

        // The stale copy still says Draft, so the domain accepts the edit, but saving it fails.
        staleCopy.AddLine("Added too late", 1m, 999m);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());

        await using var check = Open(cs);
        Invoice stored = await check.Invoices.Include(i => i.Lines).SingleAsync(i => i.Id == id);
        Assert.Equal(InvoiceStatus.Issued, stored.Status);
        Assert.Single(stored.Lines);
        Assert.Equal(450m, stored.Total);
    }

    private static FactureQrDbContext Open(string cs) => new(FactureQrDbContext.CreateOptions(cs));

    private static async Task<T[]> RunTogetherAsync<T>(string cs, IEnumerable<Guid> ids, Func<FactureQrDbContext, Guid, Task<T>> operation)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<T>[] tasks = [.. ids.Select(id => StartWhenSignalledAsync(cs, start.Task, db => operation(db, id)))];
        start.SetResult();
        return await Task.WhenAll(tasks);
    }

    // Opens the connection first, so that every operation is ready when the signal comes.
    private static async Task<T> StartWhenSignalledAsync<T>(string cs, Task signal, Func<FactureQrDbContext, Task<T>> operation)
    {
        await using var db = Open(cs);
        await db.Database.OpenConnectionAsync();
        await signal;
        return await operation(db);
    }

    private static async Task<List<Guid>> SaveDraftsAsync(string cs, int count)
    {
        await using var db = Open(cs);
        var customer = new Customer(SimonMuster);
        db.Customers.Add(customer);
        var ids = new List<Guid>();
        for (int i = 0; i < count; i++)
        {
            var draft = Invoice.CreateDraft(customer);
            draft.AddLine("Consulting", 3m, 150m);
            db.Invoices.Add(draft);
            ids.Add(draft.Id);
        }

        await db.SaveChangesAsync();
        return ids;
    }
}
