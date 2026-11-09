using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>
/// HU-04 to HU-06 over HTTP, as transactions.yaml describes them, with real
/// accounts: record (BR-01, BR-02), history newest first without deleted ones,
/// and soft delete (BR-04). Every endpoint needs a token (NFR-05).
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class TransactionsEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GET", "/api/v1/transactions")]
    [InlineData("POST", "/api/v1/transactions")]
    [InlineData("DELETE", "/api/v1/transactions/00000000-0000-7000-8000-000000000001")]
    public async Task EveryEndpoint_withoutAToken_isUnauthorized(string method, string path) // TC-013
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) };

        using var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Record_expense_isCreated_withItsLocation_andTypedLines()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.AdminAsync(api, postgres);
        var (supplies, _) = await Subcategories(client);

        using var created = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            transactionDate = "2026-11-06",
            description = "Printer paper",
            details = new[] { new { subcategoryId = supplies, amount = 85.50m }, new { subcategoryId = supplies, amount = 0.01m } },
        }, Ct);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = (await Json(created)).RootElement;
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal($"/api/v1/transactions/{id}", created.Headers.Location?.ToString());
        Assert.Equal("2026-11-06", body.GetProperty("transactionDate").GetString());
        var details = body.GetProperty("details").EnumerateArray().ToList();
        Assert.Equal([85.50m, 0.01m], details.Select(d => d.GetProperty("amount").GetDecimal()));
        Assert.All(details, d => Assert.Equal("expense", d.GetProperty("type").GetString()));
    }

    [Fact]
    public async Task Record_typeSentByTheClient_isIgnored() // BR-01
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.AdminAsync(api, postgres);
        var (supplies, _) = await Subcategories(client);

        using var created = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            transactionDate = "2026-11-06",
            description = "Paper",
            details = new[] { new { subcategoryId = supplies, amount = 1m, type = "income" } },
        }, Ct);

        Assert.Equal("expense", (await Json(created)).RootElement.GetProperty("details")[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task Record_mixingIncomeAndExpense_is422_namingTheLine() // BR-02, TC-004
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.AdminAsync(api, postgres);
        var (supplies, store) = await Subcategories(client);

        using var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            transactionDate = "2026-11-06",
            description = "Mixed",
            details = new[] { new { subcategoryId = store, amount = 100m }, new { subcategoryId = supplies, amount = 5m } },
        }, Ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = (await Json(response)).RootElement;
        Assert.Equal("BUSINESS_RULE_VIOLATED", body.GetProperty("error").GetString());
        Assert.Equal("details[1].subcategoryId", body.GetProperty("details")[0].GetProperty("field").GetString());
        using var list = await client.GetAsync(new Uri("/api/v1/transactions", UriKind.Relative), Ct);
        Assert.Equal(0, (await Json(list)).RootElement.GetProperty("meta").GetProperty("total").GetInt64());
    }

    [Fact]
    public async Task Record_badFields_is400_perField()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.AdminAsync(api, postgres);
        var (supplies, _) = await Subcategories(client);

        using var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            transactionDate = "2026-11-06",
            description = " ",
            details = new[] { new { subcategoryId = supplies, amount = 0m }, new { subcategoryId = supplies, amount = 1.005m } },
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var fields = (await Json(response)).RootElement.GetProperty("details").EnumerateArray()
            .Select(d => d.GetProperty("field").GetString());
        Assert.Equal(["description", "details[0].amount", "details[1].amount"], fields);
    }

    [Fact]
    public async Task Record_notADate_is400()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.AdminAsync(api, postgres);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/transactions", new { transactionDate = "06/11/2026", description = "x", details = Array.Empty<object>() }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", (await Json(response)).RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task History_newestFirst_paged_andADeletedOneIsGone_thenDeletingAgainIs404() // TC-005, HU-06
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.AdminAsync(api, postgres);
        var (supplies, _) = await Subcategories(client);
        var ids = new Dictionary<string, Guid>();
        foreach (var (date, description) in new[] { ("2026-10-01", "A"), ("2026-10-20", "C"), ("2026-10-15", "B") })
        {
            using var created = await client.PostAsJsonAsync("/api/v1/transactions", new
            {
                transactionDate = date,
                description,
                details = new[] { new { subcategoryId = supplies, amount = 10m } },
            }, Ct);
            ids[description] = (await Json(created)).RootElement.GetProperty("id").GetGuid();
        }

        using var page2 = await client.GetAsync(new Uri("/api/v1/transactions?page=2&limit=2", UriKind.Relative), Ct);
        var body = (await Json(page2)).RootElement;
        Assert.Equal(["A"], body.GetProperty("data").EnumerateArray().Select(t => t.GetProperty("description").GetString()));
        Assert.Equal(3, body.GetProperty("meta").GetProperty("total").GetInt64());

        using var deleted = await client.DeleteAsync(new Uri($"/api/v1/transactions/{ids["C"]}", UriKind.Relative), Ct);
        using var again = await client.DeleteAsync(new Uri($"/api/v1/transactions/{ids["C"]}", UriKind.Relative), Ct);
        using var after = await client.GetAsync(new Uri("/api/v1/transactions", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        Assert.Equal(
            ["B", "A"],
            (await Json(after)).RootElement.GetProperty("data").EnumerateArray().Select(t => t.GetProperty("description").GetString()));
    }

    [Fact]
    public async Task AnotherUser_neverSeesOrDeletesMyTransactions_butSharesTheCatalog()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var mine = await Accounts.AdminAsync(api, postgres);
        using var theirs = await Accounts.SignedInAsync(api);
        var (supplies, _) = await Subcategories(mine);
        using var created = await mine.PostAsJsonAsync("/api/v1/transactions", new
        {
            transactionDate = "2026-11-06",
            description = "Mine",
            details = new[] { new { subcategoryId = supplies, amount = 10m } },
        }, Ct);
        var id = (await Json(created)).RootElement.GetProperty("id").GetGuid();

        using var list = await theirs.GetAsync(new Uri("/api/v1/transactions", UriKind.Relative), Ct);
        using var delete = await theirs.DeleteAsync(new Uri($"/api/v1/transactions/{id}", UriKind.Relative), Ct);
        using var borrow = await theirs.PostAsJsonAsync("/api/v1/transactions", new
        {
            transactionDate = "2026-11-06",
            description = "Borrowed",
            details = new[] { new { subcategoryId = supplies, amount = 10m } },
        }, Ct);

        Assert.Equal(0, (await Json(list)).RootElement.GetProperty("meta").GetProperty("total").GetInt64());
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Created, borrow.StatusCode);
    }

    /// <summary>An expense subcategory and an income one, for the signed-in account.</summary>
    private static async Task<(Guid Supplies, Guid Store)> Subcategories(HttpClient client)
    {
        async Task<Guid> Create(string category, string type, string subcategory)
        {
            using var c = await client.PostAsJsonAsync("/api/v1/categories", new { name = category, type }, Ct);
            var categoryId = (await Json(c)).RootElement.GetProperty("id").GetGuid();
            using var s = await client.PostAsJsonAsync($"/api/v1/categories/{categoryId}/subcategories", new { name = subcategory }, Ct);
            return (await Json(s)).RootElement.GetProperty("id").GetGuid();
        }

        return (await Create("Oficina", "expense", "Papelería"), await Create("Ventas", "income", "Tienda"));
    }

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
}
