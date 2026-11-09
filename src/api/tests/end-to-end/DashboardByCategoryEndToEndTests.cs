using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>TC-025: the dashboard against the budget, globally or by category (HU-11).</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class DashboardByCategoryEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);

    private static async Task<Guid> Id(HttpResponseMessage response)
    {
        using var body = await Json(response);
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<(Guid Category, Guid Subcategory)> CatalogAsync(HttpClient admin, string name, string type = "expense")
    {
        using var category = await admin.PostAsJsonAsync("/api/v1/categories", new { name, type }, Ct);
        var id = await Id(category);
        using var subcategory = await admin.PostAsJsonAsync($"/api/v1/categories/{id}/subcategories", new { name = "Sub " + name }, Ct);
        return (id, await Id(subcategory));
    }

    private static async Task BudgetAsync(HttpClient admin, Guid category, decimal amount, DateOnly start, DateOnly end) =>
        (await admin.PostAsJsonAsync(
            "/api/v1/budgets", new { categoryId = category, amount, periodStart = Date(start), periodEnd = Date(end) }, Ct)).Dispose();

    private static async Task RecordAsync(HttpClient client, Guid subcategory, decimal amount, DateOnly? date = null) =>
        (await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            transactionDate = Date(date ?? Today),
            description = "Movimiento",
            details = new[] { new { subcategoryId = subcategory, amount } },
        }, Ct)).Dispose();

    private static async Task<JsonElement> Dashboard(HttpClient client, Guid? category = null)
    {
        var query = category is null ? string.Empty : $"?categoryId={category}";
        using var response = await client.GetAsync(new Uri("/api/v1/dashboard" + query, UriKind.Relative), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await Json(response);
        return body.RootElement.Clone();
    }

    private static void AssertBudget(JsonElement dashboard, decimal amount, decimal spent, decimal available)
    {
        var budget = dashboard.GetProperty("budget");
        Assert.Equal(amount, budget.GetProperty("amount").GetDecimal());
        Assert.Equal(spent, budget.GetProperty("spent").GetDecimal());
        Assert.Equal(available, budget.GetProperty("available").GetDecimal());
    }

    [Fact]
    public async Task AUsersTotalsAreTheirs_andTheBudgetIsTheBusinesss()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.AdminAsync(api, postgres);
        using var ana = await Accounts.UserAsync(api, postgres);
        using var leo = await Accounts.UserAsync(api, postgres);
        var office = await CatalogAsync(admin, "Oficina");
        await BudgetAsync(admin, office.Category, 500m, Today.AddDays(-5), Today.AddDays(5));
        await RecordAsync(ana, office.Subcategory, 100m);
        await RecordAsync(leo, office.Subcategory, 300m);

        var asAna = await Dashboard(ana);
        var asAdmin = await Dashboard(admin);

        Assert.Equal(100m, asAna.GetProperty("totalExpenses").GetDecimal());
        Assert.Equal(400m, asAdmin.GetProperty("totalExpenses").GetDecimal());
        AssertBudget(asAna, 500m, 400m, 100m);
        AssertBudget(asAdmin, 500m, 400m, 100m);
    }

    [Fact]
    public async Task TheFilter_narrowsEverythingToOneCategory()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.AdminAsync(api, postgres);
        var office = await CatalogAsync(admin, "Oficina");
        var rent = await CatalogAsync(admin, "Arriendo");
        await BudgetAsync(admin, office.Category, 500m, Today.AddDays(-5), Today.AddDays(5));
        await BudgetAsync(admin, rent.Category, 1000m, Today.AddDays(-5), Today.AddDays(5));
        await RecordAsync(admin, office.Subcategory, 450m);
        await RecordAsync(admin, rent.Subcategory, 1000m);

        var global = await Dashboard(admin);
        var filtered = await Dashboard(admin, office.Category);

        AssertBudget(global, 1500m, 1450m, 50m);
        AssertBudget(filtered, 500m, 450m, 50m);
        Assert.Equal(450m, filtered.GetProperty("totalExpenses").GetDecimal());
        Assert.Equal(1, filtered.GetProperty("recentTransactions").GetArrayLength());
        Assert.Equal(2, global.GetProperty("recentTransactions").GetArrayLength());
    }

    [Fact]
    public async Task OnlyBudgetsInForce_andSpendingInsideTheirPeriods_count()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.AdminAsync(api, postgres);
        var office = await CatalogAsync(admin, "Oficina");
        var sales = await CatalogAsync(admin, "Ventas", "income");
        await BudgetAsync(admin, office.Category, 500m, Today.AddDays(-5), Today.AddDays(5));
        await BudgetAsync(admin, office.Category, 999m, Today.AddDays(-40), Today.AddDays(-20));
        await BudgetAsync(admin, sales.Category, 777m, Today.AddDays(-5), Today.AddDays(5));
        await RecordAsync(admin, office.Subcategory, 50m);
        await RecordAsync(admin, office.Subcategory, 70m, Today.AddDays(-30));

        AssertBudget(await Dashboard(admin), 500m, 50m, 450m);
    }

    [Fact]
    public async Task OverBudget_isNegative_andNoBudgetInForce_isNull()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.AdminAsync(api, postgres);
        var office = await CatalogAsync(admin, "Oficina");
        var rent = await CatalogAsync(admin, "Arriendo");
        await BudgetAsync(admin, office.Category, 500m, Today.AddDays(-5), Today.AddDays(5));
        await RecordAsync(admin, office.Subcategory, 600m);

        AssertBudget(await Dashboard(admin, office.Category), 500m, 600m, -100m);
        Assert.Equal(JsonValueKind.Null, (await Dashboard(admin, rent.Category)).GetProperty("budget").ValueKind);
    }

    [Fact]
    public async Task AnUnknownCategory_isNotFound_andAMalformedOne_isBadRequest()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.UserAsync(api, postgres);

        using var unknown = await client.GetAsync(new Uri($"/api/v1/dashboard?categoryId={Guid.NewGuid()}", UriKind.Relative), Ct);
        using var malformed = await client.GetAsync(new Uri("/api/v1/dashboard?categoryId=nope", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
    }
}
