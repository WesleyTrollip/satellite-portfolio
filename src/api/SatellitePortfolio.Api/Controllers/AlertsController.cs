using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public sealed class AlertsController(AlertService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AlertEvent>>> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var alerts = await service.ListAsync(from, to, cancellationToken);
        return Ok(alerts);
    }

    [HttpGet("current")]
    public async Task<ActionResult<IReadOnlyCollection<AlertEvent>>> Current(CancellationToken cancellationToken)
    {
        var alerts = await service.ListCurrentAsync(cancellationToken);
        return Ok(alerts);
    }
}

