using System.Net;
using System.Text.Json;
using BudTack.EndToEnd.Tests.Support;
using Xunit;

namespace BudTack.EndToEnd.Tests;

/// <summary>
/// The platform behaviour every module relies on, through the one HTTP API:
/// <c>/health</c>, the error format and the correlation id.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class PlatformEndToEndTests(PostgresFixture postgres)
{
    private const string CorrelationHeader = "X-Correlation-Id";

    [Fact]
    public async Task Health_withReachableDatabase_isHealthy()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("Healthy", body.RootElement.GetProperty("checks").GetProperty("database").GetString());
    }

    [Fact]
    public async Task Health_withUnreachableDatabase_isServiceUnavailable()
    {
        await using var api = new ApiFactory("postgresql://nobody:nothing@127.0.0.1:1/missing?timeout=2");
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.Equal("Unhealthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task UnknownRoute_isNotFound_inTheErrorFormat()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/nothing-here", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.Equal("NOT_FOUND", body.RootElement.GetProperty("error").GetString());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task UnhandledError_isInternalError_quotingTheCorrelationId()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();

        using var response = await client.GetAsync(new Uri("/__test/throw", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var correlationId = Assert.Single(response.Headers.GetValues(CorrelationHeader));
        using var body = await ReadJson(response);
        Assert.Equal("INTERNAL_ERROR", body.RootElement.GetProperty("error").GetString());
        Assert.Contains(correlationId, body.RootElement.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.DoesNotContain("provoked", body.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CorrelationId_fromTheCaller_isEchoed()
    {
        await using var api = new ApiFactory(postgres.DatabaseUrl);
        using var client = api.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/health", UriKind.Relative));
        request.Headers.Add(CorrelationHeader, "support-ticket-17");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("support-ticket-17", Assert.Single(response.Headers.GetValues(CorrelationHeader)));
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
