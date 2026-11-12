using System.Net;
using System.Net.Http.Json;
using FactureQr.Api.Customers;
using FactureQr.Api.Invoices;

namespace FactureQr.Api.Tests;

[Collection(PostgreSqlGroup.Name)]
public class CustomerApiTests(PostgreSqlFixture postgres)
{
    [Fact]
    public async Task A_created_customer_is_returned_with_201_and_its_location_and_can_be_read_back()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();

        HttpResponseMessage created = await client.PostAsJsonAsync("/customers", ApiClient.SimonMuster, ApiClient.Json);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        CustomerResponse customer = await created.ReadAsync<CustomerResponse>();
        Assert.Equal($"/customers/{customer.Id}", created.Headers.Location?.ToString());
        Assert.Equal(ApiClient.SimonMuster, customer.Address);

        HttpResponseMessage read = await client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(customer, await read.ReadAsync<CustomerResponse>());
    }

    [Fact]
    public async Task Customers_are_listed_by_name()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        await client.CreateCustomerAsync(ApiClient.SimonMuster);
        await client.CreateCustomerAsync(new AddressDto("Pia-Maria Rutschmann-Schnyder", "Grosse Marktgasse", "28", "9400", "Rorschach", "CH"));

        List<CustomerResponse> customers = await client.GetFromJsonAsync<List<CustomerResponse>>("/customers", ApiClient.Json) ?? [];

        Assert.Equal(["Pia-Maria Rutschmann-Schnyder", "Simon Muster"], customers.Select(c => c.Address.Name));
    }

    [Theory]
    [InlineData("country", "Switzerland")]
    [InlineData("postalCode", "")]
    [InlineData("town", "Seldwyla\nNord")]
    public async Task An_address_that_breaks_a_QR_bill_rule_is_refused_with_400_naming_the_field(string field, string value)
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        AddressDto address = field switch
        {
            "country" => ApiClient.SimonMuster with { Country = value },
            "postalCode" => ApiClient.SimonMuster with { PostalCode = value },
            _ => ApiClient.SimonMuster with { Town = value },
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/customers", address, ApiClient.Json);

        await response.AssertInvalidFieldAsync(field);
        Assert.Empty(await client.GetFromJsonAsync<List<CustomerResponse>>("/customers", ApiClient.Json) ?? []);
    }

    [Theory]
    [InlineData("""{ "street": "Musterstrasse", "postalCode": "8000", "town": "Seldwyla", "country": "CH" }""")]
    [InlineData("""{ "name": null, "postalCode": "8000", "town": "Seldwyla", "country": "CH" }""")]
    [InlineData("""{ "name": "Simon Muster", """)]
    public async Task A_body_with_a_missing_required_value_or_broken_JSON_is_refused_with_400(string body)
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/customers", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        await response.AssertProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_unknown_customer_is_answered_with_404()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();

        await (await client.GetAsync($"/customers/{Guid.NewGuid()}")).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await client.PutAsJsonAsync($"/customers/{Guid.NewGuid()}", ApiClient.SimonMuster, ApiClient.Json)).AssertProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Changing_the_address_does_not_change_the_address_already_copied_onto_an_invoice()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());
        HttpClient client = api.CreateClient();
        CustomerResponse customer = await client.CreateCustomerAsync();
        InvoiceResponse draft = await client.CreateDraftAsync(customer.Id);
        AddressDto moved = ApiClient.SimonMuster with { Street = "Bahnhofstrasse", BuildingNumber = "7" };

        HttpResponseMessage response = await client.PutAsJsonAsync($"/customers/{customer.Id}", moved, ApiClient.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(moved, (await response.ReadAsync<CustomerResponse>()).Address);
        InvoiceResponse invoice = (await client.GetFromJsonAsync<InvoiceResponse>($"/invoices/{draft.Id}", ApiClient.Json))!;
        Assert.Equal(ApiClient.SimonMuster, invoice.BillTo);
    }
}
