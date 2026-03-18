using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/instruments")]
public sealed class InstrumentsController(InstrumentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Instrument>>> GetAll(CancellationToken cancellationToken)
    {
        var instruments = await service.ListAsync(cancellationToken);
        return Ok(instruments);
    }

    [HttpGet("{instrumentId:guid}")]
    public async Task<ActionResult<Instrument>> GetById(Guid instrumentId, CancellationToken cancellationToken)
    {
        var instrument = await service.GetByIdAsync(new InstrumentId(instrumentId), cancellationToken);
        return instrument is null ? NotFound() : Ok(instrument);
    }

    [HttpPost]
    public async Task<ActionResult<Instrument>> Create([FromBody] CreateInstrumentDto request, CancellationToken cancellationToken)
    {
        var instrument = await service.CreateAsync(
            new CreateInstrumentRequest(request.Symbol, request.Name, request.Sector, request.Currency ?? "EUR"),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { instrumentId = instrument.Id.Value }, instrument);
    }

    [HttpPut("{instrumentId:guid}")]
    public async Task<ActionResult<Instrument>> Update(
        Guid instrumentId,
        [FromBody] UpdateInstrumentDto request,
        CancellationToken cancellationToken)
    {
        var instrument = await service.UpdateAsync(
            new InstrumentId(instrumentId),
            new UpdateInstrumentRequest(request.Symbol, request.Name, request.Sector, request.Currency ?? "EUR"),
            cancellationToken);

        return instrument is null ? NotFound() : Ok(instrument);
    }
}

public sealed record CreateInstrumentDto(string Symbol, string? Name, string? Sector, string? Currency);
public sealed record UpdateInstrumentDto(string Symbol, string? Name, string? Sector, string? Currency);

