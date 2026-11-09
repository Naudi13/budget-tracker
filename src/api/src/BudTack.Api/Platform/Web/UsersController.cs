using BudTack.Identity.Api;
using BudTack.Identity.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary><c>/api/v1/users</c> (HU-09, HU-10, identity.yaml), for administrators only.</summary>
[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = BearerAuthentication.AdminOnly)]
internal sealed class UsersController(IdentityFacade identity) : ControllerBase
{
    [HttpGet]
    public Task<UserPage> List(int? page, int? limit, CancellationToken cancellationToken) =>
        identity.ListUsersAsync(page, limit, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<CreatedUser>> Create(NewUser request, CancellationToken cancellationToken)
    {
        var created = await identity.CreateUserAsync(User.UserId(), request, cancellationToken);
        return Created($"/api/v1/users/{created.User.Id}", created);
    }

    [HttpPatch("{userId:guid}")]
    public Task<User> SetActive(Guid userId, UserStatusUpdate request, CancellationToken cancellationToken) =>
        identity.SetUserActiveAsync(User.UserId(), userId, request, cancellationToken);

    [HttpDelete("{userId:guid}")]
    public async Task<NoContentResult> Delete(Guid userId, CancellationToken cancellationToken)
    {
        await identity.DeleteUserAsync(User.UserId(), userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/activation-links")]
    public async Task<ActionResult<AccountLink>> CreateActivationLink(Guid userId, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await identity.CreateActivationLinkAsync(User.UserId(), userId, cancellationToken));

    [HttpPost("{userId:guid}/password-resets")]
    public async Task<ActionResult<AccountLink>> CreatePasswordReset(Guid userId, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await identity.CreatePasswordResetAsync(User.UserId(), userId, cancellationToken));
}
