using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>TC-021 over HTTP, against the real unique index.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class RenameCategoryEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);

    private static async Task<string> Create(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/categories", new { name, type = "expense" }, Ct);
        using var body = await Json(response);
        return body.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Patch_followsTheCase() // TC-021
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.AdminAsync(api, postgres);
        using var other = await Accounts.UserAsync(api, postgres);
        var id = await Create(client, "Oficina");
        await Create(client, "Arriendo");
        var url = $"/api/v1/categories/{id}";

        using var renamed = await client.PatchAsJsonAsync(url, new { name = "Papelería" }, Ct);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        using var body = await Json(renamed);
        Assert.Equal("Papelería", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("expense", body.RootElement.GetProperty("type").GetString());
        Assert.True(body.RootElement.GetProperty("isActive").GetBoolean());

        using var taken = await client.PatchAsJsonAsync(url, new { name = "Arriendo" }, Ct);
        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);
        using var conflict = await Json(taken);
        Assert.Equal("CONFLICT", conflict.RootElement.GetProperty("error").GetString());

        using var blank = await client.PatchAsJsonAsync(url, new { name = "   " }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);

        using var empty = await client.PatchAsJsonAsync(url, new { }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        using var aUser = await other.PatchAsJsonAsync(url, new { name = "Mía" }, Ct);
        Assert.Equal(HttpStatusCode.Forbidden, aUser.StatusCode);
    }
}
