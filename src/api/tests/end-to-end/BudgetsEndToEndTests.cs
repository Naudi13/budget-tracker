using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>HU-07 over HTTP, as budgets.yaml describes it, with real accounts (TC-007, TC-019).</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class BudgetsEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GET", "/api/v1/budgets")]
    [InlineData("POST", "/api/v1/budgets")]
    [InlineData("PATCH", "/api/v1/budgets/00000000-0000-7000-8000-000000000001")]
    [InlineData("DELETE", "/api/v1/budgets/00000000-0000-7000-8000-000000000001")]
    public async Task EveryEndpoint_withoutAToken_isUnauthorized(string method, string path) // TC-013
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { amount = 1 }) };

        using var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_isCreatedWithItsLocation_andListedByCategory()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.SignedInAsync(api);
        var office = await Category(client, "Oficina");
        var travel = await Category(client, "Viajes");

        using var created = await client.PostAsJsonAsync(
            "/api/v1/budgets", new { categoryId = office, amount = 500.00m, periodStart = "2026-11-01", periodEnd = "2026-11-30" }, Ct);
        (await client.PostAsJsonAsync(
            "/api/v1/budgets", new { categoryId = travel, amount = 300m, periodStart = "2026-11-01", periodEnd = "2026-11-30" }, Ct)).Dispose();

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = (await Json(created)).RootElement;
        Assert.Equal($"/api/v1/budgets/{body.GetProperty("id").GetGuid()}", created.Headers.Location?.ToString());
        Assert.Equal("2026-11-30", body.GetProperty("periodEnd").GetString());
        using var list = await client.GetAsync(new Uri($"/api/v1/budgets?categoryId={office}", UriKind.Relative), Ct);
        var page = (await Json(list)).RootElement;
        Assert.Equal(1, page.GetProperty("meta").GetProperty("total").GetInt64());
        Assert.Equal(500m, page.GetProperty("data")[0].GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task Create_periodEndingBeforeItStarts_is422_andAZeroAmount_is400() // TC-007
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.SignedInAsync(api);
        var office = await Category(client, "Oficina");

        using var period = await client.PostAsJsonAsync(
            "/api/v1/budgets", new { categoryId = office, amount = 500m, periodStart = "2026-11-30", periodEnd = "2026-11-01" }, Ct);
        using var zero = await client.PostAsJsonAsync(
            "/api/v1/budgets", new { categoryId = office, amount = 0m, periodStart = "2026-11-01", periodEnd = "2026-11-30" }, Ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, period.StatusCode);
        var body = (await Json(period)).RootElement;
        Assert.Equal("BUSINESS_RULE_VIOLATED", body.GetProperty("error").GetString());
        Assert.Equal("periodEnd", body.GetProperty("details")[0].GetProperty("field").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal("amount", (await Json(zero)).RootElement.GetProperty("details")[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task TC019_overHttp_changeRefused_changeAccepted_categoryBlocked_deleted_categoryFree()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.SignedInAsync(api);
        var office = await Category(client, "Oficina");
        using var created = await client.PostAsJsonAsync(
            "/api/v1/budgets", new { categoryId = office, amount = 500m, periodStart = "2026-11-01", periodEnd = "2026-11-30" }, Ct);
        var id = (await Json(created)).RootElement.GetProperty("id").GetGuid();

        using var broken = await client.PatchAsJsonAsync($"/api/v1/budgets/{id}", new { periodEnd = "2026-10-15" }, Ct);
        using var changed = await client.PatchAsJsonAsync($"/api/v1/budgets/{id}", new { amount = 650m }, Ct);
        using var categoryBlocked = await client.DeleteAsync(new Uri($"/api/v1/categories/{office}", UriKind.Relative), Ct);
        using var deleted = await client.DeleteAsync(new Uri($"/api/v1/budgets/{id}", UriKind.Relative), Ct);
        using var again = await client.DeleteAsync(new Uri($"/api/v1/budgets/{id}", UriKind.Relative), Ct);
        using var categoryFree = await client.DeleteAsync(new Uri($"/api/v1/categories/{office}", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, broken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var body = (await Json(changed)).RootElement;
        Assert.Equal(650m, body.GetProperty("amount").GetDecimal());
        Assert.Equal("2026-11-30", body.GetProperty("periodEnd").GetString());
        Assert.Equal(HttpStatusCode.Conflict, categoryBlocked.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, categoryFree.StatusCode);
    }

    [Fact]
    public async Task Patch_withNothingToChange_is400()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.SignedInAsync(api);
        var office = await Category(client, "Oficina");
        using var created = await client.PostAsJsonAsync(
            "/api/v1/budgets", new { categoryId = office, amount = 500m, periodStart = "2026-11-01", periodEnd = "2026-11-30" }, Ct);
        var id = (await Json(created)).RootElement.GetProperty("id").GetGuid();

        using var empty = await client.PatchAsJsonAsync($"/api/v1/budgets/{id}", new { }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task AnotherBusiness_neverSeesOrTouchesMyBudgets()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var mine = await Accounts.SignedInAsync(api);
        using var theirs = await Accounts.SignedInAsync(api);
        var office = await Category(mine, "Oficina");
        using var created = await mine.PostAsJsonAsync(
            "/api/v1/budgets", new { categoryId = office, amount = 500m, periodStart = "2026-11-01", periodEnd = "2026-11-30" }, Ct);
        var id = (await Json(created)).RootElement.GetProperty("id").GetGuid();

        using var list = await theirs.GetAsync(new Uri("/api/v1/budgets", UriKind.Relative), Ct);
        using var patch = await theirs.PatchAsJsonAsync($"/api/v1/budgets/{id}", new { amount = 1m }, Ct);
        using var delete = await theirs.DeleteAsync(new Uri($"/api/v1/budgets/{id}", UriKind.Relative), Ct);

        Assert.Equal(0, (await Json(list)).RootElement.GetProperty("meta").GetProperty("total").GetInt64());
        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    private static async Task<Guid> Category(HttpClient client, string name)
    {
        using var created = await client.PostAsJsonAsync("/api/v1/categories", new { name, type = "expense" }, Ct);
        return (await Json(created)).RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
}
