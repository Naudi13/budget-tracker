using BudTack.Identity.Api;
using BudTack.Identity.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary><c>POST /api/v1/users</c> — sign-up (HU-01, identity.yaml <c>createUser</c>).</summary>
[ApiController]
[Route("api/v1/users")]
internal sealed class UsersController(IdentityFacade identity) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<User>> Create(NewUser request, CancellationToken cancellationToken)
    {
        var user = await identity.SignUpAsync(request, cancellationToken);
        return Created($"/api/v1/users/{user.Id}", user);
    }
}
