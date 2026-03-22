using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
public sealed class PortfolioController(PortfolioQueryService queryService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<PortfolioOverviewView>> Overview(
        [FromQuery] DateTime? asOf,
        CancellationToken cancellationToken)
    {
        var overview = await queryService.GetOverviewAsync(asOf, cancellationToken);
        return Ok(overview);
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<MonthlyPortfolioStateView>> Monthly(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        var monthly = await queryService.GetMonthlyStateAsync(year, month, cancellationToken);
        return Ok(monthly);
    }
}

