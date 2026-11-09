using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>TC-024: only an administrator manages the catalog (HU-12).</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class PermissionsEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly Guid Unknown = Guid.Parse("00000000-0000-7000-8000-000000000099");

    private static async Task<Guid> Id(HttpResponseMessage response)
    {
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
        return body.RootElement.GetProperty("id").GetGuid();
    }

    [Theory]
    [InlineData("POST", "/api/v1/categories")]
    [InlineData("PATCH", "/api/v1/categories/{id}")]
    [InlineData("DELETE", "/api/v1/categories/{id}")]
    [InlineData("POST", "/api/v1/categories/{id}/subcategories")]
    [InlineData("PATCH", "/api/v1/categories/{id}/subcategories/{id}")]
    [InlineData("DELETE", "/api/v1/categories/{id}/subcategories/{id}")]
    [InlineData("GET", "/api/v1/budgets")]
    [InlineData("POST", "/api/v1/budgets")]
    [InlineData("PATCH", "/api/v1/budgets/{id}")]
    [InlineData("DELETE", "/api/v1/budgets/{id}")]
    public async Task AUser_isForbidden_andNothingChanges(string method, string path)
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.UserAsync(api, postgres);
        using var request = new HttpRequestMessage(new HttpMethod(method), path.Replace("{id}", Unknown.ToString(), StringComparison.Ordinal))
        {
            Content = method is "GET" or "DELETE" ? null : JsonContent.Create(new { name = "X" }),
        };

        using var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AUser_readsTheCatalogAnAdministratorCreated()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.AdminAsync(api, postgres);
        using var user = await Accounts.UserAsync(api, postgres);
        using var category = await admin.PostAsJsonAsync("/api/v1/categories", new { name = "Oficina", type = "expense" }, Ct);
        var id = await Id(category);
        (await admin.PostAsJsonAsync($"/api/v1/categories/{id}/subcategories", new { name = "Papelería" }, Ct)).Dispose();

        using var categories = await user.GetAsync(new Uri("/api/v1/categories", UriKind.Relative), Ct);
        using var subcategories = await user.GetAsync(new Uri($"/api/v1/categories/{id}/subcategories", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
        Assert.Contains("Oficina", await categories.Content.ReadAsStringAsync(Ct), StringComparison.Ordinal);
        Assert.Contains("Papelería", await subcategories.Content.ReadAsStringAsync(Ct), StringComparison.Ordinal);
    }
}
