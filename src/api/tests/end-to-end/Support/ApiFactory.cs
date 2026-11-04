using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BudTack.EndToEnd.Tests.Support;

/// <summary>
/// Hosts the real <c>Program</c> — the whole deployable, as it ships — pointed at
/// the given database. Adds <see cref="ThrowingController"/> so a test can
/// provoke an unhandled error.
/// </summary>
public sealed class ApiFactory(string databaseUrl) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DATABASE_URL", databaseUrl);
        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ThrowingController).Assembly));
    }
}
