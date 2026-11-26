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

    [Fact]
    public async Task The_API_does_not_start_with_a_creditor_address_the_PDF_cannot_print()
    {
        await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync(), town: "L'Ha\u00FF-les-Roses");

        var error = Assert.Throws<InvalidOperationException>(() => api.CreateClient());
        Assert.Contains("Invoicing:Creditor", error.Message);
        Assert.Contains("Town", error.Message);
        Assert.Contains("U+00FF", error.Message);
    }

    [Fact]
    public async Task The_built_front_end_is_served_at_the_root_and_on_its_page_paths_while_unknown_API_paths_stay_404()
    {
        string webRoot = Directory.CreateTempSubdirectory("facture-qr-web-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(webRoot, "index.html"), "<!doctype html><title>Facture QR</title>");
            await using var api = new ApiFactory(await postgres.CreateMigratedDatabaseAsync(), webRoot: webRoot);
            HttpClient client = api.CreateClient();

            foreach (string path in new[] { "/", "/clients", "/clients/nouveau", "/factures/0199a0c4-0000-7000-8000-000000000000", "/parametres" })
            {
                HttpResponseMessage page = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.OK, page.StatusCode);
                Assert.Equal("text/html", page.Content.Headers.ContentType?.MediaType);
                Assert.Contains("<title>Facture QR</title>", await page.Content.ReadAsStringAsync());
            }

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/invoices/not-an-identifier")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/unknown")).StatusCode);
            Assert.Equal("application/json", (await client.GetAsync("/customers")).Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
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
        Assert.True(paths.TryGetProperty("/settings/creditor", out _));
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
