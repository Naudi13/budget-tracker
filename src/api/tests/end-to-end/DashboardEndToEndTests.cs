using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>HU-08 over HTTP, as dashboard.yaml describes it (TC-008, TC-010).</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class DashboardEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Dashboard_withoutAToken_isUnauthorized() // NFR-05
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/dashboard", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_newAccount_isAllZeros_withNoRecentTransactions()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.AdminAsync(api, postgres);

        var body = await Dashboard(client);

        Assert.Equal(0m, body.GetProperty("totalIncome").GetDecimal());
        Assert.Equal(0m, body.GetProperty("totalExpenses").GetDecimal());
        Assert.Equal(0m, body.GetProperty("netBalance").GetDecimal());
        Assert.Equal(0, body.GetProperty("recentTransactions").GetArrayLength());
    }

    [Fact]
    public async Task Dashboard_totalsAndRecentList_onRead_withinTwoSeconds() // TC-008, TC-010
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.AdminAsync(api, postgres);
        var store = await Subcategory(client, "Ventas", "income", "Tienda");
        var supplies = await Subcategory(client, "Oficina", "expense", "Papelería");
        await Record(client, "2026-11-01", "Venta", store, 1000m);
        await Record(client, "2026-11-02", "Papel", supplies, 300m, 50m);
        var mistake = await Record(client, "2026-11-03", "Error", supplies, 100m);
        (await client.DeleteAsync(new Uri($"/api/v1/transactions/{mistake}", UriKind.Relative), Ct)).Dispose();

        var started = System.Diagnostics.Stopwatch.StartNew();
        var body = await Dashboard(client);
        started.Stop();

        Assert.Equal(1000m, body.GetProperty("totalIncome").GetDecimal());
        Assert.Equal(350m, body.GetProperty("totalExpenses").GetDecimal());
        Assert.Equal(650m, body.GetProperty("netBalance").GetDecimal());
        Assert.Equal(
            ["Papel", "Venta"],
            body.GetProperty("recentTransactions").EnumerateArray().Select(t => t.GetProperty("description").GetString()));
        Assert.True(started.Elapsed < TimeSpan.FromSeconds(2), $"took {started.Elapsed}");
    }

    private static async Task<JsonElement> Dashboard(HttpClient client)
    {
        using var response = await client.GetAsync(new Uri("/api/v1/dashboard", UriKind.Relative), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
        return document.RootElement.Clone();
    }

    private static async Task<Guid> Subcategory(HttpClient client, string category, string type, string name)
    {
        using var c = await client.PostAsJsonAsync("/api/v1/categories", new { name = category, type }, Ct);
        var categoryId = (await JsonDocument.ParseAsync(await c.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct))
            .RootElement.GetProperty("id").GetGuid();
        using var s = await client.PostAsJsonAsync($"/api/v1/categories/{categoryId}/subcategories", new { name }, Ct);
        return (await JsonDocument.ParseAsync(await s.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct))
            .RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> Record(HttpClient client, string date, string description, Guid subcategory, params decimal[] amounts)
    {
        using var created = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            transactionDate = date,
            description,
            details = amounts.Select(amount => new { subcategoryId = subcategory, amount }).ToArray(),
        }, Ct);
        return (await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct))
            .RootElement.GetProperty("id").GetGuid();
    }
}
