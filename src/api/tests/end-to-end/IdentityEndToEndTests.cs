using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>
/// Accounts over HTTP, exactly as identity.yaml describes them: an administrator
/// creates them, single-use links set the password, and sign-in returns the bearer
/// token later requests carry (TC-001, TC-013, TC-014, TC-022, TC-023).
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class IdentityEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string UniqueEmail() => $"owner-{Guid.NewGuid():N}@example.com";

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);

    private static Task<HttpResponseMessage> Confirm(HttpClient client, string token, string password) =>
        client.PostAsJsonAsync("/api/v1/account-links/confirm", new { token, password }, Ct);

    private static Task<HttpResponseMessage> SignIn(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/sessions", new { email, password }, Ct);

    [Fact]
    public async Task PostUsers_asAnAdmin_createsAPendingAccount_withItsLink() // TC-022
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        var email = UniqueEmail();

        using var response = await admin.PostAsJsonAsync(
            "/api/v1/users", new { name = "Ana Owner", email = email.ToUpperInvariant(), role = "USER" }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await Json(response);
        var user = body.RootElement.GetProperty("user");
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.Equal("USER", user.GetProperty("role").GetString());
        Assert.Equal("PENDING", user.GetProperty("status").GetString());
        Assert.False(user.TryGetProperty("password", out _));
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("activation").GetProperty("token").GetString()));
        Assert.Equal($"/api/v1/users/{user.GetProperty("id").GetGuid()}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PostUsers_emailInUse_isConflict() // TC-001
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        var email = UniqueEmail();
        await Accounts.CreateAsync(admin, email);

        using var response = await admin.PostAsJsonAsync("/api/v1/users", new { name = "Otra", email, role = "USER" }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal("CONFLICT", body.RootElement.GetProperty("error").GetString());
        Assert.Equal(1L, await postgres.ScalarAsync("SELECT count(*) FROM identity.users WHERE email = $1", email));
    }

    [Fact]
    public async Task PostUsers_invalidFields_isValidationErrorWithEveryField()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);

        using var response = await admin.PostAsJsonAsync("/api/v1/users", new { name = " ", email = "nope", role = "OWNER" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal(
            ["name", "email", "role"],
            body.RootElement.GetProperty("details").EnumerateArray().Select(d => d.GetProperty("field").GetString()));
    }

    [Theory]
    [InlineData("{\"email\":\"x\"", "application/json", HttpStatusCode.BadRequest)]
    [InlineData("email=x", "text/plain", HttpStatusCode.UnsupportedMediaType)]
    public async Task PostSessions_brokenJsonOrNotJson_isAValidationErrorInOurFormat(
        string content, string mediaType, HttpStatusCode expected)
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.PostAsync(
            new Uri("/api/v1/sessions", UriKind.Relative), new StringContent(content, null, mediaType), Ct);

        Assert.Equal(expected, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal("VALIDATION_ERROR", body.RootElement.GetProperty("error").GetString());
        Assert.False(body.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task ActivationThenSignIn_storesOnlyAHash_andNeverLogsThePassword() // TC-014, TC-023
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        var token = await Accounts.CreateAsync(admin, email);

        using var confirmed = await Confirm(client, token, Accounts.Password);
        using var session = await SignIn(client, email, Accounts.Password);
        (await SignIn(client, email, "Wrong" + Accounts.Password)).Dispose();

        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
        Assert.Equal(HttpStatusCode.Created, session.StatusCode);
        var stored = (string?)await postgres.ScalarAsync("SELECT password_hash FROM identity.users WHERE email = $1", email);
        Assert.StartsWith("$2a$12$", stored, StringComparison.Ordinal);
        Assert.NotEmpty(api.LogLines);
        Assert.DoesNotContain(api.LogLines, line => line.Contains(Accounts.Password, StringComparison.Ordinal));
        Assert.DoesNotContain(api.LogLines, line => line.Contains(token, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AccountLink_worksOnce_andAPendingAccountCannotSignIn() // TC-022, TC-023
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        var token = await Accounts.CreateAsync(admin, email);

        using var pending = await SignIn(client, email, Accounts.Password);
        using var first = await Confirm(client, token, Accounts.Password);
        using var second = await Confirm(client, token, "An0therPass!");

        Assert.Equal(HttpStatusCode.Unauthorized, pending.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        using var body = await Json(second);
        Assert.Equal("token", body.RootElement.GetProperty("details")[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task AnExpiredLink_answersLikeAnyInvalidOne() // TC-023
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        var token = await Accounts.CreateAsync(admin, email);
        await postgres.ScalarAsync(
            "UPDATE identity.account_tokens t SET expires_at = now() - interval '1 minute' FROM identity.users u WHERE u.id = t.user_id AND u.email = $1 RETURNING t.id",
            email);

        using var expired = await Confirm(client, token, Accounts.Password);
        using var unknown = await Confirm(client, "not-a-token", Accounts.Password);

        Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
        Assert.Equal(await unknown.Content.ReadAsStringAsync(Ct), await expired.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task PasswordReset_replacesThePassword_withoutTheAdminEverSeeingOne() // TC-023
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        var token = await Accounts.CreateAsync(admin, email);
        (await Confirm(client, token, Accounts.Password)).Dispose();
        var id = (Guid)(await postgres.ScalarAsync("SELECT id FROM identity.users WHERE email = $1", email))!;

        using var reset = await admin.PostAsync(new Uri($"/api/v1/users/{id}/password-resets", UriKind.Relative), null, Ct);
        using var link = await Json(reset);
        using var confirmed = await Confirm(client, link.RootElement.GetProperty("token").GetString()!, "An0therPass!");
        using var old = await SignIn(client, email, Accounts.Password);
        using var fresh = await SignIn(client, email, "An0therPass!");

        Assert.Equal(HttpStatusCode.Created, reset.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, old.StatusCode);
        Assert.Equal(HttpStatusCode.Created, fresh.StatusCode);
    }

    [Fact]
    public async Task ActivationLinks_stateConflicts_andUnknownAccounts()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        var email = UniqueEmail();
        var token = await Accounts.CreateAsync(admin, email);
        var id = (Guid)(await postgres.ScalarAsync("SELECT id FROM identity.users WHERE email = $1", email))!;

        using var resetOfPending = await admin.PostAsync(new Uri($"/api/v1/users/{id}/password-resets", UriKind.Relative), null, Ct);
        using var again = await admin.PostAsync(new Uri($"/api/v1/users/{id}/activation-links", UriKind.Relative), null, Ct);
        using var unknown = await admin.PostAsync(new Uri($"/api/v1/users/{Guid.NewGuid()}/activation-links", UriKind.Relative), null, Ct);
        using var oldLink = await Confirm(admin, token, Accounts.Password);

        Assert.Equal(HttpStatusCode.Conflict, resetOfPending.StatusCode);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, oldLink.StatusCode);
    }

    [Fact]
    public async Task PostSessions_rightPassword_returnsABearerTokenTheApiAccepts()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        await Accounts.ActivateAsync(api, await Accounts.CreateAsync(admin, email));
        var id = (Guid)(await postgres.ScalarAsync("SELECT id FROM identity.users WHERE email = $1", email))!;

        using var response = await SignIn(client, email, Accounts.Password);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var session = await Json(response);
        Assert.Equal("Bearer", session.RootElement.GetProperty("tokenType").GetString());
        Assert.Equal(3600, session.RootElement.GetProperty("expiresIn").GetInt32());

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", session.RootElement.GetProperty("token").GetString());
        using var probe = await client.PostAsync(new Uri("/__test/protected", UriKind.Relative), null, Ct);
        Assert.Equal(HttpStatusCode.OK, probe.StatusCode);
        using var who = await Json(probe);
        Assert.Equal(id.ToString(), who.RootElement.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task PostSessions_wrongPasswordOrUnknownEmail_isUnauthorized()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        await Accounts.ActivateAsync(api, await Accounts.CreateAsync(admin, email));

        using var wrong = await SignIn(client, email, "Wrong-Pass-123");
        using var unknown = await SignIn(client, UniqueEmail(), Accounts.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        using var body = await Json(wrong);
        Assert.Equal("UNAUTHORIZED", body.RootElement.GetProperty("error").GetString());
        Assert.Equal("The email or password is not correct.", body.RootElement.GetProperty("message").GetString());
        Assert.False(body.RootElement.TryGetProperty("details", out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer not-a-token")]
    [InlineData("Basic b3duZXI6cGFzcw==")]
    [InlineData("Bearer eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJzdWIiOiIwMTkzYTFiMi0wMDAwLTcwMDAtODAwMC0wMDAwMDAwMDAwMDEiLCJpc3MiOiJidWQtdGFjay1hcGkiLCJhdWQiOiJidWQtdGFjayIsImV4cCI6NDEwMjQ0NDgwMH0.")]
    public async Task AnyOtherWrite_withoutAValidToken_isUnauthorized(string? authorization) // TC-013
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        if (authorization is not null)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorization);
        }

        using var response = await client.PostAsync(new Uri("/__test/protected", UriKind.Relative), null, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal("UNAUTHORIZED", body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Health_needsNoToken()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
