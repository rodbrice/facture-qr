using System.Net;
using System.Text.Json;

namespace FactureQr.Api.Tests;

[Collection(PostgreSqlGroup.Name)]
public class InfrastructureTests(PostgreSqlFixture postgres)
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, IndentSize = 2 };

    [Fact]
    public async Task Health_is_200_when_the_database_answers()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());

        HttpResponseMessage response = await api.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_is_503_when_the_database_does_not_answer()
    {
        await using var api = new ApiFactory("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2");

        HttpResponseMessage response = await api.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task The_API_does_not_start_with_an_invalid_creditor()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync(), iban: "CH00 0000 0000 0000 0000 0");

        var error = Assert.Throws<InvalidOperationException>(() => api.CreateClient());
        Assert.Contains("Invoicing:Creditor", error.Message);
    }

    /// <summary>
    /// The OpenAPI document served by the API is committed under docs/api so it
    /// can be read without running the API. This test fails when the two differ;
    /// set FACTUREQR_UPDATE_OPENAPI=1 to rewrite the committed file.
    /// </summary>
    [Fact]
    public async Task The_committed_OpenAPI_document_matches_the_one_the_API_serves()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync());

        HttpResponseMessage response = await api.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string served = Normalise(await response.Content.ReadAsStringAsync());
        string path = Path.Combine(RepositoryRoot(), "docs", "api", "openapi.json");
        if (Environment.GetEnvironmentVariable("FACTUREQR_UPDATE_OPENAPI") == "1")
        {
            await File.WriteAllTextAsync(path, served);
        }

        Assert.Equal(served, Normalise(await File.ReadAllTextAsync(path)));
        using JsonDocument document = JsonDocument.Parse(served);
        JsonElement paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/invoices/{id}/pdf", out _));
        Assert.True(paths.TryGetProperty("/customers", out _));
    }

    // Indented, with a final line break, so that the committed file is readable and diffs well.
    private static string Normalise(string json) =>
        JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement, Indented) + "\n";

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FactureQr.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root (FactureQr.sln) was not found.");
    }
}
