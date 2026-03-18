using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/prices/snapshots")]
public sealed class PriceSnapshotsController(PriceSnapshotService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PriceSnapshot>>> List(
        [FromQuery] Guid? instrumentId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var snapshots = await service.ListAsync(
            instrumentId.HasValue ? new InstrumentId(instrumentId.Value) : null,
            from,
            to,
            cancellationToken);
        return Ok(snapshots);
    }

    [HttpPost]
    public async Task<ActionResult<PriceSnapshot>> Upsert([FromBody] UpsertPriceSnapshotDto request, CancellationToken cancellationToken)
    {
        var snapshot = await service.UpsertAsync(
            new UpsertPriceSnapshotRequest(
                request.InstrumentId,
                request.Date,
                request.ClosePriceAmount,
                request.Source),
            cancellationToken);

        return Ok(snapshot);
    }
}

public sealed record UpsertPriceSnapshotDto(
    Guid InstrumentId,
    DateOnly Date,
    decimal ClosePriceAmount,
    PriceSnapshotSource Source);

