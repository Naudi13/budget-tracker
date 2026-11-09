using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BudTack.EndToEnd.Tests.Support;

/// <summary>
/// Hosts the real <c>Program</c> — the whole deployable, as it ships — pointed at
/// the given database. Adds the test-only controllers of this folder, and keeps
/// every log line written, so a test can assert what was never logged (TC-014).
/// </summary>
public sealed class ApiFactory(string databaseUrl) : WebApplicationFactory<Program>
{
    public const string JwtSecret = "end-to-end-secret-end-to-end-secret-0123";

    public ConcurrentQueue<string> LogLines { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DATABASE_URL", databaseUrl);
        builder.UseSetting("AUTH_JWT_SECRET", JwtSecret);
        builder.UseSetting("AUTH_JWT_EXPIRY_SECONDS", "3600");
        builder.ConfigureLogging(logging => logging.AddProvider(new CapturingLoggerProvider(LogLines)));
        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ThrowingController).Assembly));
    }
}
