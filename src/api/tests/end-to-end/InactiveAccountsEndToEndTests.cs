using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>TC-027: inactivating and deleting accounts (HU-13, BR-09, ADR-011).</summary>
[Collection(PostgresTestGroup.Name)]
public sealed class InactiveAccountsEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Uri Categories => new("/api/v1/categories", UriKind.Relative);

    private static string UniqueEmail() => $"owner-{Guid.NewGuid():N}@example.com";

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);

    private async Task<Guid> IdOf(string email) =>
        (Guid)(await postgres.ScalarAsync("SELECT id FROM identity.users WHERE email = $1", email))!;

    private static Task<HttpResponseMessage> SetActive(HttpClient client, Guid id, bool isActive) =>
        client.PatchAsJsonAsync($"/api/v1/users/{id}", new { isActive }, Ct);

    [Fact]
    public async Task Inactivating_endsTheSessionAtOnce_andActivatingRestoresTheAccount()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var anna = api.CreateClient();
        var email = UniqueEmail();
        await Accounts.ActivateAsync(api, await Accounts.CreateAsync(admin, email));
        await Accounts.SignInAsync(anna, email, Accounts.Password);
        var id = await IdOf(email);

        using var before = await anna.GetAsync(Categories, Ct);
        using var off = await SetActive(admin, id, false);
        using var afterwards = await anna.GetAsync(Categories, Ct);
        using var signIn = await anna.PostAsJsonAsync("/api/v1/sessions", new { email, password = Accounts.Password }, Ct);
        using var on = await SetActive(admin, id, true);
        using var again = await anna.PostAsJsonAsync("/api/v1/sessions", new { email, password = Accounts.Password }, Ct);

        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        using var offBody = await Json(off);
        Assert.Equal("INACTIVE", offBody.RootElement.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, afterwards.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, signIn.StatusCode);
        using var onBody = await Json(on);
        Assert.Equal("ACTIVE", onBody.RootElement.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task Inactivating_cancelsThePendingLinks()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        var token = await Accounts.CreateAsync(admin, email);

        (await SetActive(admin, await IdOf(email), false)).Dispose();
        using var confirm = await client.PostAsJsonAsync("/api/v1/account-links/confirm", new { token, password = Accounts.Password }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
    }

    [Fact]
    public async Task AnAdministrator_cannotInactivateThemselves()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        var email = UniqueEmail();
        using var first = await Accounts.NewAdminAsync(api, postgres);
        await Accounts.CreateAsync(first, email, "ADMIN");
        var id = await IdOf(email);
        await Accounts.ActivateAsync(api, await LinkOf(first, id));
        using var second = api.CreateClient();
        await Accounts.SignInAsync(second, email, Accounts.Password);

        using var response = await SetActive(second, id, false);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<string> LinkOf(HttpClient admin, Guid id)
    {
        using var response = await admin.PostAsync(new Uri($"/api/v1/users/{id}/activation-links", UriKind.Relative), null, Ct);
        using var body = await Json(response);
        return body.RootElement.GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task Delete_onlyANeverActivatedAccount_andItsLinkStopsWorking()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var pendingEmail = UniqueEmail();
        var activeEmail = UniqueEmail();
        var token = await Accounts.CreateAsync(admin, pendingEmail);
        await Accounts.ActivateAsync(api, await Accounts.CreateAsync(admin, activeEmail));
        var pending = await IdOf(pendingEmail);

        using var deleted = await admin.DeleteAsync(new Uri($"/api/v1/users/{pending}", UriKind.Relative), Ct);
        using var again = await admin.DeleteAsync(new Uri($"/api/v1/users/{pending}", UriKind.Relative), Ct);
        using var active = await admin.DeleteAsync(new Uri($"/api/v1/users/{await IdOf(activeEmail)}", UriKind.Relative), Ct);
        using var link = await client.PostAsJsonAsync("/api/v1/account-links/confirm", new { token, password = Accounts.Password }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, active.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, link.StatusCode);
    }

    [Fact]
    public async Task AUser_isForbidden()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var user = await Accounts.UserAsync(api, postgres);
        var someone = Guid.NewGuid();

        using var patch = await SetActive(user, someone, false);
        using var delete = await user.DeleteAsync(new Uri($"/api/v1/users/{someone}", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}
