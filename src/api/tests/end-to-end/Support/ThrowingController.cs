using Microsoft.AspNetCore.Mvc;

namespace BudTack.EndToEnd.Tests.Support;

/// <summary>Test-only endpoint that fails, to prove the unhandled-error path.</summary>
[ApiController]
[Route("__test/throw")]
public sealed class ThrowingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        throw new InvalidOperationException($"provoked by an end-to-end test on {Request.Path}");
}
