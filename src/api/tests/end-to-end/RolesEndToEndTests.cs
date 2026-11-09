using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>HU-09 over HTTP (TC-020), and the CORS and Swagger of ADR-008.</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class RolesEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Uri Users => new("/api/v1/users", UriKind.Relative);

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);

    [Fact]
    public async Task GetUsers_withoutAToken_isUnauthorized() // TC-020, step 1
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.GetAsync(Users, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal("UNAUTHORIZED", body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetUsers_asAUser_isForbidden() // TC-020, step 2
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.UserAsync(api, postgres);

        using var response = await client.GetAsync(Users, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal("FORBIDDEN", body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetUsers_asAnAdmin_listsTheAccountsWithoutTheirHashes() // TC-020, step 3
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = await Accounts.NewAdminAsync(api, postgres);

        using var response = await client.GetAsync(new Uri("/api/v1/users?limit=100", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await Json(response);
        var users = body.RootElement.GetProperty("data").EnumerateArray().ToList();
        Assert.Contains(users, user => user.GetProperty("role").GetString() == "ADMIN");
        Assert.All(users, user => Assert.False(user.TryGetProperty("passwordHash", out _)));
        var emails = users.Select(user => user.GetProperty("email").GetString()!).ToList();
        Assert.Equal(emails.Order(StringComparer.Ordinal), emails);
    }

    [Fact]
    public async Task PostUsers_withoutAdmin_isRefused_andNobodySignsUp() // TC-020, step 4
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var anonymous = api.CreateClient();
        using var user = await Accounts.UserAsync(api, postgres);
        var body = new { name = "Eve", email = "eve@example.com", role = "ADMIN" };

        using var noToken = await anonymous.PostAsJsonAsync("/api/v1/users", body, Ct);
        using var asUser = await user.PostAsJsonAsync("/api/v1/users", body, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, asUser.StatusCode);
    }

    [Theory]
    [InlineData("http://localhost:8080", true)]
    [InlineData("https://evil.example", false)]
    public async Task Preflight_onlyThePortalsOriginIsAllowed(string origin, bool allowed) // ADR-008
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, Users);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request, Ct);

        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task SwaggerUi_isPublic_andDocumentsTheApi() // ADR-008
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var page = await client.GetAsync(new Uri("/swagger-ui.html", UriKind.Relative), Ct);
        using var document = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(HttpStatusCode.OK, document.StatusCode);
        using var body = await Json(document);
        Assert.True(body.RootElement.GetProperty("paths").TryGetProperty("/api/v1/users", out _));
    }
}
