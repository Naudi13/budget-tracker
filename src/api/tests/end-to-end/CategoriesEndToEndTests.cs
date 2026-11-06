using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>
/// HU-02 and HU-03 over HTTP, as categories.yaml describes them, with real
/// accounts and bearer tokens: one business never sees another's categories
/// (rest-conventions.md), and every write needs a token (NFR-05, TC-013).
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class CategoriesEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GET", "/api/v1/categories")]
    [InlineData("POST", "/api/v1/categories")]
    [InlineData("DELETE", "/api/v1/categories/00000000-0000-7000-8000-000000000001")]
    [InlineData("PATCH", "/api/v1/categories/00000000-0000-7000-8000-000000000001")]
    [InlineData("GET", "/api/v1/categories/00000000-0000-7000-8000-000000000001/subcategories")]
    [InlineData("POST", "/api/v1/categories/00000000-0000-7000-8000-000000000001/subcategories")]
    [InlineData("DELETE", "/api/v1/categories/00000000-0000-7000-8000-000000000001/subcategories/00000000-0000-7000-8000-000000000002")]
    [InlineData("PATCH", "/api/v1/categories/00000000-0000-7000-8000-000000000001/subcategories/00000000-0000-7000-8000-000000000002")]
    public async Task EveryEndpoint_withoutAToken_isUnauthorized(string method, string path) // TC-013
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = JsonContent.Create(new { name = "Oficina", type = "expense", isActive = false }),
        };

        using var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHORIZED", (await Json(response)).RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateCategory_isCreatedWithItsLocation_andListed()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await SignedIn(api);

        using var created = await client.PostAsJsonAsync("/api/v1/categories", new { name = " Oficina ", type = "expense" }, Ct);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = (await Json(created)).RootElement;
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal("Oficina", body.GetProperty("name").GetString());
        Assert.Equal("expense", body.GetProperty("type").GetString());
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.Equal($"/api/v1/categories/{id}", created.Headers.Location?.ToString());

        using var list = await client.GetAsync(new Uri("/api/v1/categories?type=expense", UriKind.Relative), Ct);
        var page = (await Json(list)).RootElement;
        Assert.Equal(id, page.GetProperty("data")[0].GetProperty("id").GetGuid());
        Assert.Equal(1, page.GetProperty("meta").GetProperty("page").GetInt32());
        Assert.Equal(20, page.GetProperty("meta").GetProperty("limit").GetInt32());
        Assert.Equal(1, page.GetProperty("meta").GetProperty("total").GetInt64());
    }

    [Fact]
    public async Task CreateCategory_sameNameAndType_isConflict_otherTypeIsNot()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await SignedIn(api);
        (await client.PostAsJsonAsync("/api/v1/categories", new { name = "Otros", type = "expense" }, Ct)).Dispose();

        using var duplicate = await client.PostAsJsonAsync("/api/v1/categories", new { name = "OTROS", type = "expense" }, Ct);
        using var otherType = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Otros", type = "income" }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var error = (await Json(duplicate)).RootElement;
        Assert.Equal("CONFLICT", error.GetProperty("error").GetString());
        Assert.False(error.TryGetProperty("details", out _));
        Assert.Equal(HttpStatusCode.Created, otherType.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_invalid_isValidationError_perField()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await SignedIn(api);

        using var response = await client.PostAsJsonAsync("/api/v1/categories", new { name = " ", type = "savings" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = (await Json(response)).RootElement;
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("error").GetString());
        Assert.Equal(["name", "type"], body.GetProperty("details").EnumerateArray().Select(d => d.GetProperty("field").GetString()));
    }

    [Theory]
    [InlineData("?limit=101", "limit")]
    [InlineData("?page=0", "page")]
    [InlineData("?type=all", "type")]
    public async Task ListCategories_badQuery_isValidationError(string query, string field)
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await SignedIn(api);

        using var response = await client.GetAsync(new Uri("/api/v1/categories" + query, UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(field, (await Json(response)).RootElement.GetProperty("details")[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task AnotherBusiness_neverSeesOrTouchesMyCategories()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var mine = await SignedIn(api);
        using var theirs = await SignedIn(api);
        using var created = await mine.PostAsJsonAsync("/api/v1/categories", new { name = "Privada", type = "expense" }, Ct);
        var id = (await Json(created)).RootElement.GetProperty("id").GetGuid();

        using var list = await theirs.GetAsync(new Uri("/api/v1/categories", UriKind.Relative), Ct);
        using var delete = await theirs.DeleteAsync(new Uri($"/api/v1/categories/{id}", UriKind.Relative), Ct);
        using var patch = await theirs.PatchAsJsonAsync($"/api/v1/categories/{id}", new { isActive = false }, Ct);
        using var subcategory = await theirs.PostAsJsonAsync($"/api/v1/categories/{id}/subcategories", new { name = "x" }, Ct);

        Assert.Equal(0, (await Json(list)).RootElement.GetProperty("meta").GetProperty("total").GetInt64());
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, subcategory.StatusCode);
        Assert.Equal("NOT_FOUND", (await Json(delete)).RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task DeleteCategory_withASubcategory_isConflict_thenDeactivate_thenDeleteBoth() // TC-003
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await SignedIn(api);
        using var created = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Oficina", type = "expense" }, Ct);
        var categoryId = (await Json(created)).RootElement.GetProperty("id").GetGuid();
        using var sub = await client.PostAsJsonAsync(
            $"/api/v1/categories/{categoryId}/subcategories", new { name = "Papelería" }, Ct);
        Assert.Equal(HttpStatusCode.Created, sub.StatusCode);
        var subcategoryId = (await Json(sub)).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(
            $"/api/v1/categories/{categoryId}/subcategories/{subcategoryId}", sub.Headers.Location?.ToString());

        using var refused = await client.DeleteAsync(new Uri($"/api/v1/categories/{categoryId}", UriKind.Relative), Ct);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        using var deactivated = await client.PatchAsJsonAsync($"/api/v1/categories/{categoryId}", new { isActive = false }, Ct);
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        Assert.False((await Json(deactivated)).RootElement.GetProperty("isActive").GetBoolean());

        using var subOff = await client.PatchAsJsonAsync(
            $"/api/v1/categories/{categoryId}/subcategories/{subcategoryId}", new { isActive = false }, Ct);
        Assert.False((await Json(subOff)).RootElement.GetProperty("isActive").GetBoolean());

        using var subList = await client.GetAsync(new Uri($"/api/v1/categories/{categoryId}/subcategories", UriKind.Relative), Ct);
        Assert.Equal(1, (await Json(subList)).RootElement.GetProperty("meta").GetProperty("total").GetInt64());

        using var subDelete = await client.DeleteAsync(
            new Uri($"/api/v1/categories/{categoryId}/subcategories/{subcategoryId}", UriKind.Relative), Ct);
        using var categoryDelete = await client.DeleteAsync(new Uri($"/api/v1/categories/{categoryId}", UriKind.Relative), Ct);
        Assert.Equal(HttpStatusCode.NoContent, subDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, categoryDelete.StatusCode);
    }

    [Fact]
    public async Task CreateSubcategory_duplicateName_isConflict() // TC-002
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await SignedIn(api);
        using var created = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Oficina", type = "expense" }, Ct);
        var categoryId = (await Json(created)).RootElement.GetProperty("id").GetGuid();
        (await client.PostAsJsonAsync($"/api/v1/categories/{categoryId}/subcategories", new { name = "Papelería" }, Ct)).Dispose();

        using var duplicate = await client.PostAsJsonAsync(
            $"/api/v1/categories/{categoryId}/subcategories", new { name = "papelería" }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task PatchCategory_withoutIsActive_isValidationError_andABadId_isNotFound()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await SignedIn(api);
        using var created = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Oficina", type = "expense" }, Ct);
        var id = (await Json(created)).RootElement.GetProperty("id").GetGuid();

        using var missing = await client.PatchAsJsonAsync($"/api/v1/categories/{id}", new { }, Ct);
        using var notAGuid = await client.PatchAsJsonAsync("/api/v1/categories/not-a-guid", new { isActive = true }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("isActive", (await Json(missing)).RootElement.GetProperty("details")[0].GetProperty("field").GetString());
        Assert.Equal(HttpStatusCode.NotFound, notAGuid.StatusCode);
    }

    /// <summary>A client signed in as a brand-new account.</summary>
    private static async Task<HttpClient> SignedIn(ApiFactory api)
    {
        var client = api.CreateClient();
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/v1/users", new { name = "Ana", email, password = "Str0ngPass!23" }, Ct)).Dispose();
        using var session = await client.PostAsJsonAsync("/api/v1/sessions", new { email, password = "Str0ngPass!23" }, Ct);
        var token = (await Json(session)).RootElement.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
}
