using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/prices/snapshots")]
public sealed class PriceSnapshotsController(
    PriceSnapshotService service,
    IInstrumentRepository instruments,
    IPriceSourceLookupRepository priceSources) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PriceSnapshotListItemDto>>> List(
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
        var allInstruments = await instruments.ListAsync(cancellationToken);
        var allPriceSources = await priceSources.ListAsync(null, null, 0, 1000, cancellationToken);
        var instrumentMap = allInstruments.ToDictionary(x => x.Id, x => x);
        var sourceMap = allPriceSources.ToDictionary(x => x.Id, x => x);

        var response = snapshots.Select(snapshot =>
        {
            instrumentMap.TryGetValue(snapshot.InstrumentId, out var instrument);
            sourceMap.TryGetValue(snapshot.PriceSourceLookupId, out var source);
            return new PriceSnapshotListItemDto(
                snapshot.Id.Value,
                snapshot.InstrumentId.Value,
                instrument?.Symbol ?? "UNKNOWN",
                instrument is null ? "UNKNOWN" : string.IsNullOrWhiteSpace(instrument.Name) ? instrument.Symbol : $"{instrument.Symbol} - {instrument.Name}",
                snapshot.Date,
                snapshot.ClosePriceAmount,
                snapshot.PriceSourceLookupId.Value,
                source?.Name ?? "Unknown");
        }).ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<PriceSnapshot>> Upsert([FromBody] UpsertPriceSnapshotDto request, CancellationToken cancellationToken)
    {
        var snapshot = await service.UpsertAsync(
            new UpsertPriceSnapshotRequest(
                request.InstrumentId,
                request.Date,
                request.ClosePriceAmount,
                request.PriceSourceLookupId),
            cancellationToken);

        return Ok(snapshot);
    }
}

public sealed record UpsertPriceSnapshotDto(
    Guid InstrumentId,
    DateOnly Date,
    decimal ClosePriceAmount,
    Guid PriceSourceLookupId);

public sealed record PriceSnapshotListItemDto(
    Guid Id,
    Guid InstrumentId,
    string InstrumentSymbol,
    string InstrumentLabel,
    DateOnly Date,
    decimal ClosePriceAmount,
    Guid PriceSourceLookupId,
    string PriceSourceName);

