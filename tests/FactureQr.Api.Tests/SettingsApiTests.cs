using System.Net;
using FactureQr.Api.Customers;
using FactureQr.Api.Settings;

namespace FactureQr.Api.Tests;

[Collection(PostgreSqlGroup.Name)]
public class SettingsApiTests(PostgreSqlFixture postgres)
{
    [Theory]
    [InlineData(ApiFactory.QrIban, true)]
    [InlineData(ApiFactory.PlainIban, false)]
    public async Task The_creditor_from_the_configuration_is_returned(string iban, bool isQrIban)
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync(), iban: iban);

        HttpResponseMessage response = await api.CreateClient().GetAsync("/settings/creditor");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CreditorResponse creditor = await response.ReadAsync<CreditorResponse>();
        Assert.Equal(new AddressDto("Max Muster & Söhne", "8000", "Seldwyla", "CH", "Musterstrasse", "123"), creditor.Address);
        Assert.Equal(iban, creditor.Iban);
        Assert.Equal(isQrIban, creditor.IsQrIban);
    }
}
