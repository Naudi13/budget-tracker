using BudTack.Dashboard.Api;
using BudTack.Dashboard.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace BudTack.Api.Platform.Web;

/// <summary><c>GET /api/v1/dashboard</c> (HU-08, HU-11, dashboard.yaml), for the signed-in user (NFR-05).</summary>
[ApiController]
[Route("api/v1/dashboard")]
internal sealed class DashboardController(DashboardFacade dashboard) : ControllerBase
{
    [HttpGet]
    public Task<DashboardView> Get(Guid? categoryId, CancellationToken cancellationToken) =>
        dashboard.GetDashboardAsync(User.Viewer(), categoryId, cancellationToken);
}
