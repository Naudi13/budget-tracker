using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BudTack.EndToEnd.Tests.Support;

/// <summary>Accounts for end-to-end tests, signed in over HTTP like a real user.</summary>
internal static class Accounts
{
    public const string Password = "Str0ngPass!23";

    // BCrypt of the seeded administrator's public test password (Identity003_SeedUsers).
    private const string AdminPassword = "Admin-2026!";
    private const string AdminHash = "$2a$12$kWKh4iUORy6GAZEhMHINt.BdOo1UsDtMmPMqoXq8iVSpRkRTICIlK";

    /// <summary>
    /// A client carrying the token of a new ADMIN, with an empty catalog: names are
    /// unique across the whole business (ADR-009).
    /// </summary>
    public static async Task<HttpClient> AdminAsync(ApiFactory api, PostgresFixture postgres)
    {
        await postgres.ScalarAsync(
            "TRUNCATE transactions.budgets, transactions.transaction_details, transactions.transactions, transactions.subcategories, transactions.categories");
        return await NewAdminAsync(api, postgres);
    }

    /// <summary>A client carrying the token of a new, activated USER.</summary>
    public static async Task<HttpClient> UserAsync(ApiFactory api, PostgresFixture postgres)
    {
        using var admin = await NewAdminAsync(api, postgres);
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        await ActivateAsync(api, await CreateAsync(admin, email));
        var client = api.CreateClient();
        await SignInAsync(client, email, Password);
        return client;
    }

    /// <summary>A new ADMIN straight in the database, the way the first one arrives.</summary>
    public static async Task<HttpClient> NewAdminAsync(ApiFactory api, PostgresFixture postgres)
    {
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        await postgres.ScalarAsync(
            "INSERT INTO identity.users (id, name, email, password_hash, role) VALUES ($1, 'Admin', $2, $3, 'ADMIN') RETURNING id",
            Guid.CreateVersion7(), email, AdminHash);
        var client = api.CreateClient();
        await SignInAsync(client, email, AdminPassword);
        return client;
    }

    /// <summary>POST /users as <paramref name="admin"/>; the activation token.</summary>
    public static async Task<string> CreateAsync(HttpClient admin, string email, string role = "USER")
    {
        var ct = TestContext.Current.CancellationToken;
        using var created = await admin.PostAsJsonAsync("/api/v1/users", new { name = "Ana", email, role }, ct);
        using var body = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return body.RootElement.GetProperty("activation").GetProperty("token").GetString()!;
    }

    public static async Task ActivateAsync(ApiFactory api, string token, string password = Password)
    {
        using var anonymous = api.CreateClient();
        (await anonymous.PostAsJsonAsync(
            "/api/v1/account-links/confirm", new { token, password }, TestContext.Current.CancellationToken)).Dispose();
    }

    /// <summary>Signs <paramref name="client"/> in; the session's role.</summary>
    public static async Task<string> SignInAsync(HttpClient client, string email, string password)
    {
        var ct = TestContext.Current.CancellationToken;
        using var session = await client.PostAsJsonAsync("/api/v1/sessions", new { email, password }, ct);
        using var body = await JsonDocument.ParseAsync(await session.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("token").GetString());
        return body.RootElement.GetProperty("role").GetString()!;
    }
}
