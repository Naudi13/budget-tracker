using BudTack.Identity.Api;
using BudTack.Identity.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary><c>POST /api/v1/account-links/confirm</c> (HU-10): the link is the credential, so it needs no token.</summary>
[ApiController]
[Route("api/v1/account-links")]
internal sealed class AccountLinksController(IdentityFacade identity) : ControllerBase
{
    [HttpPost("confirm")]
    [AllowAnonymous]
    public async Task<NoContentResult> Confirm(LinkConfirmation request, CancellationToken cancellationToken)
    {
        await identity.ConfirmLinkAsync(request, cancellationToken);
        return NoContent();
    }
}
