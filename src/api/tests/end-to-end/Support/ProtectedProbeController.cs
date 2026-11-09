using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.EndToEnd.Tests.Support;

/// <summary>
/// Test-only write endpoint with no authorization attribute at all: it must
/// still demand a token, because protection is the default (NFR-05, TC-013).
/// </summary>
[ApiController]
[Route("__test/protected")]
public sealed class ProtectedProbeController : ControllerBase
{
    [HttpPost]
    public IActionResult Post() => Ok(new { userId = User.FindFirstValue(ClaimTypes.NameIdentifier) });
}
