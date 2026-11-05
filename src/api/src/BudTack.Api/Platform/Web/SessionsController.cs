using BudTack.Identity.Api;
using BudTack.Identity.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// <c>POST /api/v1/sessions</c> — sign-in (HU-01, identity.yaml <c>createSession</c>).
/// A session is not a stored resource, so its <c>201</c> points at the
/// collection; sign-out is the client discarding the token.
/// </summary>
[ApiController]
[Route("api/v1/sessions")]
internal sealed class SessionsController(IdentityFacade identity) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<Session>> Create(Credentials request, CancellationToken cancellationToken)
    {
        var session = await identity.SignInAsync(request, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Created("/api/v1/sessions", session);
    }
}
