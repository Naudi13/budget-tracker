using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>
/// RNF-02 over HTTP: after 5 failed sign-ins with one email, the sixth is refused
/// with 429 even with the right password, and other emails are not affected.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class SignInAttemptsEndToEndTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string UniqueEmail() => $"attempts-{Guid.NewGuid():N}@example.com";

    private static Task<HttpResponseMessage> SignIn(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/v1/sessions", new { email, password }, Ct);

    [Fact]
    public async Task FiveFailures_thenEvenTheRightPassword_isTooManyRequests()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var email = UniqueEmail();
        await Accounts.ActivateAsync(api, await Accounts.CreateAsync(admin, email));

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var failed = await SignIn(client, email, "Wrong-Pass-123");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        using var blocked = await SignIn(client, email, Accounts.Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        var retryAfter = int.Parse(Assert.Single(blocked.Headers.GetValues("Retry-After")), System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(retryAfter, 1, 15 * 60);
        using var body = await JsonDocument.ParseAsync(await blocked.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
        Assert.Equal("TOO_MANY_REQUESTS", body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ABlockedEmail_doesNotBlockAnother()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var admin = await Accounts.NewAdminAsync(api, postgres);
        using var client = api.CreateClient();
        var blockedEmail = UniqueEmail();
        var otherEmail = UniqueEmail();
        await Accounts.ActivateAsync(api, await Accounts.CreateAsync(admin, otherEmail));

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            (await SignIn(client, blockedEmail, "Wrong-Pass-123")).Dispose();
        }

        using var blocked = await SignIn(client, blockedEmail, "Wrong-Pass-123");
        using var other = await SignIn(client, otherEmail, Accounts.Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }
}
