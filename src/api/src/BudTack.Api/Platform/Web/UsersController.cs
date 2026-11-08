using BudTack.Identity.Api;
using BudTack.Identity.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary>
/// <c>/api/v1/users</c> (identity.yaml): anyone signs up (HU-01); only an
/// administrator lists the accounts (HU-09).
/// </summary>
[ApiController]
[Route("api/v1/users")]
internal sealed class UsersController(IdentityFacade identity) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = BearerAuthentication.AdminOnly)]
    public Task<UserPage> List(int? page, int? limit, CancellationToken cancellationToken) =>
        identity.ListUsersAsync(page, limit, cancellationToken);

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<User>> Create(NewUser request, CancellationToken cancellationToken)
    {
        var user = await identity.SignUpAsync(request, cancellationToken);
        return Created($"/api/v1/users/{user.Id}", user);
    }
}
