using System.Security.Claims;
using System.Text.Encodings.Web;
using BudTack.Identity.Api;
using BudTack.Identity.Api.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// Every request is authenticated once, at the edge, by asking
/// <see cref="IdentityFacade"/> what its bearer token stands for
/// (security-policy.md). Every controller action requires a signed-in user
/// unless it says <c>[AllowAnonymous]</c> — today only <c>POST /sessions</c> and
/// <c>POST /account-links/confirm</c>; <c>GET /health</c> is not a controller and is open —
/// so a new write endpoint is protected without anyone remembering to
/// (NFR-05, TC-013). A path that matches no endpoint stays a 404.
/// </summary>
internal static class BearerAuthentication
{
    public const string Scheme = "Bearer";

    /// <summary>The policy of the endpoints only an administrator may call (ADR-007).</summary>
    public const string AdminOnly = "AdminOnly";

    public static IServiceCollection AddBearerAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(Scheme).AddScheme<AuthenticationSchemeOptions, Handler>(Scheme, configureOptions: null);
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(Scheme).RequireAuthenticatedUser().Build())
            .AddPolicy(AdminOnly, new AuthorizationPolicyBuilder(Scheme).RequireAuthenticatedUser().RequireRole(Roles.Admin).Build());
        return services;
    }

    /// <summary>Every controller action demands the default policy unless it is <c>[AllowAnonymous]</c>.</summary>
    public static ControllerActionEndpointConventionBuilder MapProtectedControllers(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapControllers().RequireAuthorization();

    /// <summary>The signed-in user's id, set by <see cref="Handler"/> from the token's <c>sub</c>.</summary>
    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The request is not authenticated."));

    private sealed class Handler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IdentityFacade identity)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        private const string Prefix = "Bearer ";

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? header = Request.Headers.Authorization;
            if (string.IsNullOrEmpty(header))
            {
                return AuthenticateResult.NoResult();
            }

            if (!header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail("The Authorization header is not a bearer token.");
            }

            var user = await identity.AuthenticateAsync(header[Prefix.Length..].Trim());
            if (user is null)
            {
                return AuthenticateResult.Fail("The bearer token is not valid or has expired.");
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role)], Scheme.Name));
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
    }
}
