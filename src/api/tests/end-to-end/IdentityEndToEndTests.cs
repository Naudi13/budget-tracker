using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>
/// HU-01 over HTTP, exactly as identity.yaml describes it: sign-up, sign-in,
/// the bearer token on later requests, and the 401 every other write gives
/// without one (TC-001, TC-013, TC-014).
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class IdentityEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string UniqueEmail() => $"owner-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task PostUsers_newAccount_isCreatedWithItsLocation()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        var email = UniqueEmail();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/users", new { name = "Ana Owner", email = email.ToUpperInvariant(), password = "Str0ngPass!23" }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await Json(response);
        var id = body.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Ana Owner", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
        Assert.False(body.RootElement.TryGetProperty("password", out _));
        Assert.False(body.RootElement.TryGetProperty("passwordHash", out _));
        Assert.Equal($"/api/v1/users/{id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PostUsers_emailInUse_isConflict() // TC-001
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        (await SignUp(client, email, "Str0ngPass!23")).Dispose();

        using var response = await SignUp(client, email, "An0therPass!");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal("CONFLICT", body.RootElement.GetProperty("error").GetString());
        Assert.Equal("An account already uses this email. Sign in instead.", body.RootElement.GetProperty("message").GetString());
        Assert.False(body.RootElement.TryGetProperty("details", out _));
        Assert.Equal(1L, await postgres.ScalarAsync("SELECT count(*) FROM identity.users WHERE email = $1", email));
    }

    [Fact]
    public async Task PostUsers_invalidFields_isValidationErrorWithEveryField()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/users", new { name = " ", email = "nope", password = "short" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal("VALIDATION_ERROR", body.RootElement.GetProperty("error").GetString());
        Assert.Equal(
            ["name", "email", "password"],
            body.RootElement.GetProperty("details").EnumerateArray().Select(d => d.GetProperty("field").GetString()));
    }

    [Fact]
    public async Task PostUsers_noBody_isValidationError()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.PostAsync(
            new Uri("/api/v1/users", UriKind.Relative), new StringContent("", null, "application/json"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal("VALIDATION_ERROR", body.RootElement.GetProperty("error").GetString());
    }

    [Theory]
    [InlineData("{\"name\":\"x\"", "application/json", HttpStatusCode.BadRequest)]
    [InlineData("name=x", "text/plain", HttpStatusCode.UnsupportedMediaType)]
    public async Task PostUsers_brokenJsonOrNotJson_isAValidationErrorInOurFormat(
        string content, string mediaType, HttpStatusCode expected)
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.PostAsync(
            new Uri("/api/v1/users", UriKind.Relative), new StringContent(content, null, mediaType), Ct);

        Assert.Equal(expected, response.StatusCode);
        using var body = await Json(response);
        Assert.Equal("VALIDATION_ERROR", body.RootElement.GetProperty("error").GetString());
        Assert.False(body.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task PostUsers_storesOnlyAHash_andNeverLogsThePassword() // TC-014
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        const string password = "Str0ngPass!23";

        (await SignUp(client, email, password)).Dispose();
        (await SignIn(client, email, password)).Dispose();
        (await SignIn(client, email, "Wrong" + password)).Dispose();

        var stored = (string?)await postgres.ScalarAsync("SELECT password_hash FROM identity.users WHERE email = $1", email);
        Assert.NotNull(stored);
        Assert.DoesNotContain(password, stored, StringComparison.Ordinal);
        Assert.StartsWith("$2a$12$", stored, StringComparison.Ordinal);
        Assert.NotEmpty(api.LogLines);
        Assert.DoesNotContain(api.LogLines, line => line.Contains(password, StringComparison.Ordinal));
    }

    [Fact]
    public async Task PostSessions_rightPassword_returnsABearerTokenTheApiAccepts()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        using var signUp = await SignUp(client, email, "Str0ngPass!23");
        using var user = await Json(signUp);

        using var response = await SignIn(client, email, "Str0ngPass!23");

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
        Assert.Equal(user.RootElement.GetProperty("id").GetString(), who.RootElement.GetProperty("userId").GetString());
    }

    [Theory]
    [InlineData("Wrong-Pass-123")]
    [InlineData(null)]
    public async Task PostSessions_wrongPasswordOrUnknownEmail_isUnauthorized(string? password)
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        if (password is not null)
        {
            (await SignUp(client, email, "Str0ngPass!23")).Dispose();
        }

        using var response = await SignIn(client, email, password ?? "Str0ngPass!23");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = await Json(response);
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

    private static Task<HttpResponseMessage> SignUp(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/users", new { name = "Ana Owner", email, password }, Ct);

    private static Task<HttpResponseMessage> SignIn(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/sessions", new { email, password }, Ct);

    private static async Task<JsonDocument> Json(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
}
