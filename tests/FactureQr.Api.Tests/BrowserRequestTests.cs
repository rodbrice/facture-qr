using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FactureQr.Api.Customers;
using FactureQr.Api.Invoices;
using FactureQr.Core.Invoicing;

namespace FactureQr.Api.Tests;

/// <summary>
/// The two protections of a local API without authentication (ADR-0007):
/// requests from another site that would change data are refused, and only
/// the local host names are answered.
/// </summary>
[Collection(PostgreSqlGroup.Name)]
public class BrowserRequestTests(PostgreSqlFixture postgres)
{
    [Theory]
    [InlineData("cross-site")]
    [InlineData("same-site")]
    public async Task A_request_from_another_site_that_would_change_data_is_refused_with_403_and_changes_nothing(string site)
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse draft = await client.CreateDraftAsync(customer.Id, new LineRequest("Analyse", 1m, 100m));

        // An empty POST is what a page of another site can send without a CORS preflight.
        using var issue = new HttpRequestMessage(HttpMethod.Post, $"/invoices/{draft.Id}/issue");
        issue.Headers.Add("Sec-Fetch-Site", site);
        using var create = new HttpRequestMessage(HttpMethod.Post, "/customers") { Content = JsonContent.Create(ApiClient.SimonMuster, options: ApiClient.Json) };
        create.Headers.Add("Sec-Fetch-Site", site);

        JsonElement problem = await (await client.SendAsync(issue)).AssertProblemAsync(HttpStatusCode.Forbidden);
        Assert.Equal("Requests that change data are accepted only from the pages of this application.", problem.GetProperty("detail").GetString());
        await (await client.SendAsync(create)).AssertProblemAsync(HttpStatusCode.Forbidden);

        Assert.Equal(InvoiceStatus.Draft, (await (await client.GetAsync($"/invoices/{draft.Id}")).ReadAsync<InvoiceResponse>()).Status);
        Assert.Single(await (await client.GetAsync("/customers")).ReadAsync<List<CustomerResponse>>());
    }

    [Theory]
    [InlineData("same-origin")]
    [InlineData("none")]
    [InlineData(null)]
    public async Task A_request_from_the_application_s_own_pages_the_address_bar_or_a_client_without_the_header_is_accepted(string? site)
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse draft = await client.CreateDraftAsync(customer.Id, new LineRequest("Analyse", 1m, 100m));

        using var issue = new HttpRequestMessage(HttpMethod.Post, $"/invoices/{draft.Id}/issue");
        if (site is not null)
        {
            issue.Headers.Add("Sec-Fetch-Site", site);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(issue)).StatusCode);
    }

    [Fact]
    public async Task Reading_from_another_site_is_not_refused_by_the_check()
    {
        // The browser itself keeps another site from reading the answer (no CORS headers).
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        using var request = new HttpRequestMessage(HttpMethod.Get, "/customers");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        HttpResponseMessage response = await api.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("localhost:8080", HttpStatusCode.OK)]
    [InlineData("127.0.0.1:8080", HttpStatusCode.OK)]
    [InlineData("[::1]:8080", HttpStatusCode.OK)]
    [InlineData("attacker.example:8080", HttpStatusCode.BadRequest)]
    public async Task Only_the_local_host_names_are_answered(string host, HttpStatusCode expected)
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = host;

        HttpResponseMessage response = await api.CreateClient().SendAsync(request);

        Assert.Equal(expected, response.StatusCode);
    }
}
