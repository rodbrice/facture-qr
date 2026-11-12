using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FactureQr.Api.Customers;
using FactureQr.Api.Invoices;

namespace FactureQr.Api.Tests;

/// <summary>Small helpers over <see cref="HttpClient"/> for the calls most tests repeat.</summary>
public static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static readonly AddressDto SimonMuster = new("Simon Muster", "Musterstrasse", "1", "8000", "Seldwyla", "CH");

    public static async Task<CustomerResponse> CreateCustomerAsync(this HttpClient client, AddressDto? address = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/customers", address ?? SimonMuster, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>(Json))!;
    }

    public static async Task<InvoiceResponse> CreateDraftAsync(this HttpClient client, Guid customerId, params LineRequest[] lines)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(customerId, lines), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<InvoiceResponse>(Json))!;
    }

    public static async Task<InvoiceResponse> CreateIssuedAsync(this HttpClient client, DateOnly? issueDate = null, DateOnly? dueDate = null)
    {
        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse draft = await client.CreateDraftAsync(customer.Id, new LineRequest("Analyse", 2m, 150m), new LineRequest("Développement", 7.5m, 120m));
        HttpResponseMessage response = await client.PostAsJsonAsync($"/invoices/{draft.Id}/issue", new IssueRequest(issueDate, dueDate), Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<InvoiceResponse>(Json))!;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    /// <summary>Checks that the response is a ProblemDetails body with the given status.</summary>
    public static async Task<JsonElement> AssertProblemAsync(this HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        return problem;
    }

    /// <summary>Checks that the response is a 400 validation problem with an error for the given field.</summary>
    public static async Task<string> AssertInvalidFieldAsync(this HttpResponseMessage response, string field)
    {
        JsonElement problem = await response.AssertProblemAsync(HttpStatusCode.BadRequest);
        JsonElement errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out JsonElement messages), $"No error for '{field}' in {errors}.");
        return messages[0].GetString()!;
    }
}
