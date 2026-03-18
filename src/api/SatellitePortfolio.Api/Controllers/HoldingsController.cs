using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/holdings")]
public sealed class HoldingsController(PortfolioQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<HoldingView>>> List(
        [FromQuery] DateTime? asOf,
        CancellationToken cancellationToken)
    {
        var holdings = await queryService.GetHoldingsAsync(asOf, cancellationToken);
        return Ok(holdings);
    }

    [HttpGet("{instrumentId:guid}")]
    public async Task<ActionResult<HoldingView>> Get(
        Guid instrumentId,
        [FromQuery] DateTime? asOf,
        CancellationToken cancellationToken)
    {
        var holding = await queryService.GetHoldingAsync(new InstrumentId(instrumentId), asOf, cancellationToken);
        return holding is null ? NotFound() : Ok(holding);
    }
}

