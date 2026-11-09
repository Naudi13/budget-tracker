using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>Who sees and deletes which transactions (HU-05, HU-06, TC-026).</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class MovementsByRoleEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Uri Transactions => new("/api/v1/transactions", UriKind.Relative);

    private static Uri Deleted => new("/api/v1/transactions?deleted=true", UriKind.Relative);

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);

    private static async Task<Guid> Id(HttpResponseMessage response)
    {
        using var body = await Json(response);
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> SubcategoryAsync(HttpClient admin)
    {
        using var category = await admin.PostAsJsonAsync("/api/v1/categories", new { name = "Oficina", type = "expense" }, Ct);
        var id = await Id(category);
        using var subcategory = await admin.PostAsJsonAsync($"/api/v1/categories/{id}/subcategories", new { name = "Papelería" }, Ct);
        return await Id(subcategory);
    }

    private static async Task<Guid> RecordAsync(HttpClient client, Guid subcategory, string description)
    {
        using var created = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            transactionDate = "2026-11-06",
            description,
            details = new[] { new { subcategoryId = subcategory, amount = 10m } },
        }, Ct);
        return await Id(created);
    }

    private static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Guid id, string reason) =>
        client.PostAsJsonAsync($"/api/v1/transactions/{id}/deletion", new { reason }, Ct);

    [Fact]
    public async Task AUser_seesOnlyTheirs_andAnAdmin_seesAll_withWhoRecordedIt()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.AdminAsync(api, postgres);
        using var ana = await Accounts.UserAsync(api, postgres);
        using var leo = await Accounts.UserAsync(api, postgres);
        var subcategory = await SubcategoryAsync(admin);
        await RecordAsync(ana, subcategory, "De Ana");
        await RecordAsync(leo, subcategory, "De Leo");

        using var asAna = await Json(await ana.GetAsync(Transactions, Ct));
        using var asAdmin = await Json(await admin.GetAsync(Transactions, Ct));

        var mine = Assert.Single(asAna.RootElement.GetProperty("data").EnumerateArray());
        Assert.Equal("De Ana", mine.GetProperty("description").GetString());
        Assert.False(mine.TryGetProperty("recordedBy", out _));
        var all = asAdmin.RootElement.GetProperty("data").EnumerateArray().ToList();
        Assert.Equal(2, all.Count);
        Assert.All(all, row => Assert.Equal("Ana", row.GetProperty("recordedBy").GetString()));
    }

    [Fact]
    public async Task Deleting_needsAReason_onlyTheOwnerOrAnAdmin_andOnlyAnAdminReviewsIt() // TC-026
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.AdminAsync(api, postgres);
        using var ana = await Accounts.UserAsync(api, postgres);
        using var leo = await Accounts.UserAsync(api, postgres);
        var subcategory = await SubcategoryAsync(admin);
        var id = await RecordAsync(ana, subcategory, "De Ana");
        var other = await RecordAsync(ana, subcategory, "Otra de Ana");

        using var noReason = await DeleteAsync(ana, id, "x");
        using var notTheirs = await DeleteAsync(leo, id, "Monto equivocado");
        using var theirs = await DeleteAsync(ana, id, "Monto equivocado");
        using var byAdmin = await DeleteAsync(admin, other, "Duplicado");
        using var forbidden = await ana.GetAsync(Deleted, Ct);
        using var review = await Json(await admin.GetAsync(Deleted, Ct));

        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notTheirs.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, theirs.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var reasons = review.RootElement.GetProperty("data").EnumerateArray()
            .Select(row => row.GetProperty("deletion").GetProperty("reason").GetString());
        Assert.Equal(["Duplicado", "Monto equivocado"], reasons.Order(StringComparer.Ordinal));
    }
}
