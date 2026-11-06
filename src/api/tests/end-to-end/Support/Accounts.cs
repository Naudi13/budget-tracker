using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BudTack.EndToEnd.Tests.Support;

/// <summary>Brand-new accounts for end-to-end tests, signed in over HTTP like a real user.</summary>
internal static class Accounts
{
    /// <summary>A client carrying the bearer token of a new account.</summary>
    public static async Task<HttpClient> SignedInAsync(ApiFactory api)
    {
        var ct = TestContext.Current.CancellationToken;
        var client = api.CreateClient();
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/v1/users", new { name = "Ana", email, password = "Str0ngPass!23" }, ct)).Dispose();
        using var session = await client.PostAsJsonAsync("/api/v1/sessions", new { email, password = "Str0ngPass!23" }, ct);
        using var body = await JsonDocument.ParseAsync(await session.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("token").GetString());
        return client;
    }
}
