using System.Net;
using System.Net.Http.Json;
using FactureQr.Api.Customers;
using FactureQr.Api.Invoices;
using FactureQr.Core.Invoicing;

namespace FactureQr.Api.Tests;

[Collection(PostgreSqlGroup.Name)]
public class InvoiceApiTests(PostgreSqlFixture postgres)
{
    [Fact]
    public async Task A_draft_is_created_edited_issued_and_marked_as_paid()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();

        HttpResponseMessage created = await client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(customer.Id, [new("Analyse", 2m, 150m)]), ApiClient.Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        InvoiceResponse draft = await created.ReadAsync<InvoiceResponse>();
        Assert.Equal($"/invoices/{draft.Id}", created.Headers.Location?.ToString());
        Assert.Equal(InvoiceStatus.Draft, draft.Status);
        Assert.Null(draft.Number);
        Assert.Equal(ApiClient.SimonMuster, draft.BillTo);
        Assert.Equal(300m, draft.Total);

        HttpResponseMessage added = await client.PostAsJsonAsync($"/invoices/{draft.Id}/lines", new LineRequest("Développement", 7.5m, 120m), ApiClient.Json);
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        InvoiceResponse withTwoLines = await added.ReadAsync<InvoiceResponse>();
        Assert.Equal([1, 2], withTwoLines.Lines.Select(l => l.Position));
        Assert.Equal(1200m, withTwoLines.Total);

        HttpResponseMessage removed = await client.DeleteAsync($"/invoices/{draft.Id}/lines/{withTwoLines.Lines[0].Id}");
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        InvoiceResponse withOneLine = await removed.ReadAsync<InvoiceResponse>();
        Assert.Equal(["Développement"], withOneLine.Lines.Select(l => l.Description));
        Assert.Equal(900m, withOneLine.Total);

        // Without a body, the issue date is today in Switzerland and the due date 30 days later.
        HttpResponseMessage issued = await client.PostAsync($"/invoices/{draft.Id}/issue", null);
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        InvoiceResponse invoice = await issued.ReadAsync<InvoiceResponse>();
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal("2026-0001", invoice.Number);
        Assert.Equal(ApiFactory.Today, invoice.IssueDate);
        Assert.Equal(ApiFactory.Today.AddDays(30), invoice.DueDate);

        HttpResponseMessage paid = await client.PostAsJsonAsync($"/invoices/{draft.Id}/mark-paid", new MarkPaidRequest(ApiFactory.Today), ApiClient.Json);
        Assert.Equal(HttpStatusCode.OK, paid.StatusCode);
        Assert.Equal(InvoiceStatus.Paid, (await paid.ReadAsync<InvoiceResponse>()).Status);

        InvoiceResponse stored = (await client.GetFromJsonAsync<InvoiceResponse>($"/invoices/{draft.Id}", ApiClient.Json))!;
        Assert.Equal(InvoiceStatus.Paid, stored.Status);
        Assert.Equal(ApiFactory.Today, stored.PaidOn);
        Assert.Equal(900m, stored.Total);
    }

    [Fact]
    public async Task Numbers_follow_each_other_within_a_year_and_restart_in_a_new_year()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();

        InvoiceResponse first = await client.CreateIssuedAsync(new DateOnly(2026, 12, 30));
        InvoiceResponse second = await client.CreateIssuedAsync(new DateOnly(2026, 12, 31));
        InvoiceResponse third = await client.CreateIssuedAsync(new DateOnly(2027, 1, 4), new DateOnly(2027, 2, 3));

        Assert.Equal(["2026-0001", "2026-0002", "2027-0001"], new[] { first.Number!, second.Number!, third.Number! });
    }

    [Fact]
    public async Task Drafts_issued_through_the_API_at_the_same_time_get_distinct_numbers_without_gaps()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();
        var drafts = new List<Guid>();
        for (int i = 0; i < 20; i++)
        {
            drafts.Add((await client.CreateDraftAsync(customer.Id, new LineRequest("Analyse", 1m, 100m))).Id);
        }

        HttpResponseMessage[] responses = await Task.WhenAll(drafts.Select(id => client.PostAsync($"/invoices/{id}/issue", null)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        string?[] numbers = await Task.WhenAll(responses.Select(async r => (await r.ReadAsync<InvoiceResponse>()).Number));
        Assert.Equal(Enumerable.Range(1, 20).Select(n => $"2026-{n:D4}"), numbers.Order());
    }

    [Fact]
    public async Task The_list_carries_the_status_and_whether_each_invoice_is_overdue()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse draft = await client.CreateDraftAsync(customer.Id);
        InvoiceResponse overdue = await client.CreateIssuedAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1));
        InvoiceResponse current = await client.CreateIssuedAsync(ApiFactory.Today);
        InvoiceResponse paid = await client.CreateIssuedAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1));
        await client.PostAsJsonAsync($"/invoices/{paid.Id}/mark-paid", new MarkPaidRequest(new DateOnly(2026, 10, 15)), ApiClient.Json);

        List<InvoiceSummary> all = await client.GetFromJsonAsync<List<InvoiceSummary>>("/invoices", ApiClient.Json) ?? [];

        Assert.Equal([paid.Id, current.Id, overdue.Id, draft.Id], all.Select(i => i.Id));
        Assert.Equal([InvoiceStatus.Paid, InvoiceStatus.Issued, InvoiceStatus.Issued, InvoiceStatus.Draft], all.Select(i => i.Status));
        Assert.Equal([false, false, true, false], all.Select(i => i.Overdue));
        Assert.Equal("Simon Muster", all[0].CustomerName);
    }

    [Fact]
    public async Task Requests_that_name_an_unknown_invoice_are_answered_with_404()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        string unknown = $"/invoices/{Guid.NewGuid()}";

        await (await client.GetAsync(unknown)).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await client.PostAsJsonAsync($"{unknown}/lines", new LineRequest("Analyse", 1m, 1m), ApiClient.Json)).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await client.DeleteAsync($"{unknown}/lines/{Guid.NewGuid()}")).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await client.PostAsync($"{unknown}/issue", null)).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await client.PostAsync($"{unknown}/mark-paid", null)).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await client.GetAsync($"{unknown}/pdf")).AssertProblemAsync(HttpStatusCode.NotFound);

        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse draft = await client.CreateDraftAsync(customer.Id);
        await (await client.DeleteAsync($"/invoices/{draft.Id}/lines/{Guid.NewGuid()}")).AssertProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_requests_are_refused_with_400_and_change_nothing()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse draft = await client.CreateDraftAsync(customer.Id, new LineRequest("Analyse", 1m, 100m));

        await (await client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(Guid.NewGuid(), null), ApiClient.Json)).AssertInvalidFieldAsync("customerId");
        await (await client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(customer.Id, [new("Analyse", 1m, 1m), new("Trop précis", 1.0001m, 1m)]), ApiClient.Json)).AssertInvalidFieldAsync("lines[1]");
        await (await client.PostAsync("/invoices", new StringContent($$"""{ "customerId": "{{customer.Id}}", "lines": [null] }""", System.Text.Encoding.UTF8, "application/json"))).AssertInvalidFieldAsync("lines[0]");
        await (await client.PostAsJsonAsync($"/invoices/{draft.Id}/lines", new LineRequest(" ", 1m, 1m), ApiClient.Json)).AssertInvalidFieldAsync("line");
        await (await client.PostAsJsonAsync($"/invoices/{draft.Id}/lines", new LineRequest("Analyse", 1m, 1_000_000_000m), ApiClient.Json)).AssertInvalidFieldAsync("line");
        await (await client.PostAsJsonAsync($"/invoices/{draft.Id}/issue", new IssueRequest(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 1)), ApiClient.Json)).AssertInvalidFieldAsync("dueDate");

        InvoiceResponse unchanged = (await client.GetFromJsonAsync<InvoiceResponse>($"/invoices/{draft.Id}", ApiClient.Json))!;
        Assert.Equal(InvoiceStatus.Draft, unchanged.Status);
        Assert.Single(unchanged.Lines);
        Assert.Single(await client.GetFromJsonAsync<List<InvoiceSummary>>("/invoices", ApiClient.Json) ?? []);

        InvoiceResponse issued = await client.CreateIssuedAsync(new DateOnly(2026, 11, 2));
        await (await client.PostAsJsonAsync($"/invoices/{issued.Id}/mark-paid", new MarkPaidRequest(new DateOnly(2026, 11, 1)), ApiClient.Json)).AssertInvalidFieldAsync("paidOn");
        await (await client.PostAsJsonAsync($"/invoices/{issued.Id}/mark-paid", new MarkPaidRequest(ApiFactory.Today.AddDays(1)), ApiClient.Json)).AssertInvalidFieldAsync("paidOn");
        Assert.Equal(InvoiceStatus.Issued, (await client.GetFromJsonAsync<InvoiceResponse>($"/invoices/{issued.Id}", ApiClient.Json))!.Status);
    }

    [Fact]
    public async Task Requests_the_state_of_the_invoice_does_not_allow_are_refused_with_409()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse empty = await client.CreateDraftAsync(customer.Id);
        InvoiceResponse issued = await client.CreateIssuedAsync();

        // A draft without lines cannot be issued, has no PDF and cannot be marked as paid.
        await (await client.PostAsync($"/invoices/{empty.Id}/issue", null)).AssertProblemAsync(HttpStatusCode.Conflict);
        await (await client.GetAsync($"/invoices/{empty.Id}/pdf")).AssertProblemAsync(HttpStatusCode.Conflict);
        await (await client.PostAsync($"/invoices/{empty.Id}/mark-paid", null)).AssertProblemAsync(HttpStatusCode.Conflict);

        // An issued invoice cannot change or be issued again.
        await (await client.PostAsync($"/invoices/{issued.Id}/issue", null)).AssertProblemAsync(HttpStatusCode.Conflict);
        await (await client.PostAsJsonAsync($"/invoices/{issued.Id}/lines", new LineRequest("Analyse", 1m, 1m), ApiClient.Json)).AssertProblemAsync(HttpStatusCode.Conflict);
        await (await client.DeleteAsync($"/invoices/{issued.Id}/lines/{issued.Lines[0].Id}")).AssertProblemAsync(HttpStatusCode.Conflict);

        // A paid invoice is final.
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/invoices/{issued.Id}/mark-paid", null)).StatusCode);
        await (await client.PostAsync($"/invoices/{issued.Id}/mark-paid", null)).AssertProblemAsync(HttpStatusCode.Conflict);

        InvoiceResponse stored = (await client.GetFromJsonAsync<InvoiceResponse>($"/invoices/{issued.Id}", ApiClient.Json))!;
        Assert.Equal(issued.Lines, stored.Lines);
        Assert.Equal(issued.Number, stored.Number);

        // The refused issue of the empty draft did not use up a number.
        InvoiceResponse next = await client.CreateIssuedAsync();
        Assert.Equal("2026-0002", next.Number);
    }

    [Fact]
    public async Task A_description_with_a_character_the_PDF_cannot_print_is_refused_with_400_naming_it()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse draft = await client.CreateDraftAsync(customer.Id);

        HttpResponseMessage added = await client.PostAsJsonAsync($"/invoices/{draft.Id}/lines", new LineRequest("Analyse \U0001F600", 1m, 100m), ApiClient.Json);
        HttpResponseMessage created = await client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(customer.Id, [new("Analyse", 1m, 1m), new("Conseil \U0001F600", 1m, 1m)]), ApiClient.Json);

        Assert.Contains("U+1F600", await added.AssertInvalidFieldAsync("line"));
        Assert.Contains("U+1F600", await created.AssertInvalidFieldAsync("lines[1]"));
        Assert.Empty((await client.GetFromJsonAsync<InvoiceResponse>($"/invoices/{draft.Id}", ApiClient.Json))!.Lines);
        Assert.Single(await client.GetFromJsonAsync<List<InvoiceSummary>>("/invoices", ApiClient.Json) ?? []);
    }

    [Fact]
    public async Task A_line_that_would_not_fit_on_the_invoice_page_is_refused_with_409()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse draft = await client.CreateDraftAsync(customer.Id);
        string longDescription = string.Join(' ', Enumerable.Repeat("Développement de la fonction", 6));

        HttpResponseMessage response;
        int added = 0;
        while ((response = await client.PostAsJsonAsync($"/invoices/{draft.Id}/lines", new LineRequest(longDescription, 1m, 10m), ApiClient.Json)).StatusCode == HttpStatusCode.OK)
        {
            added++;
            Assert.True(added < 50, "The page never filled up.");
        }

        await response.AssertProblemAsync(HttpStatusCode.Conflict);
        InvoiceResponse stored = (await client.GetFromJsonAsync<InvoiceResponse>($"/invoices/{draft.Id}", ApiClient.Json))!;
        Assert.Equal(added, stored.Lines.Count);
        Assert.True(added > 1);

        // The full draft can still be issued and printed.
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/invoices/{draft.Id}/issue", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/invoices/{draft.Id}/pdf")).StatusCode);
    }
}
