using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/theses")]
public sealed class ThesesController(ThesisService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<InvestmentThesis>>> List(CancellationToken cancellationToken)
    {
        var theses = await service.ListAsync(cancellationToken);
        return Ok(theses);
    }

    [HttpGet("{thesisId:guid}")]
    public async Task<ActionResult<InvestmentThesis>> GetById(Guid thesisId, CancellationToken cancellationToken)
    {
        var thesis = await service.GetByIdAsync(new ThesisId(thesisId), cancellationToken);
        return thesis is null ? NotFound() : Ok(thesis);
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentThesis>> Create([FromBody] CreateThesisDto request, CancellationToken cancellationToken)
    {
        var thesis = await service.CreateAsync(
            new CreateThesisRequest(request.Title, request.Body, request.Status, request.InstrumentId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { thesisId = thesis.Id.Value }, thesis);
    }

    [HttpPut("{thesisId:guid}")]
    public async Task<ActionResult<InvestmentThesis>> Update(
        Guid thesisId,
        [FromBody] UpdateThesisDto request,
        CancellationToken cancellationToken)
    {
        var thesis = await service.UpdateAsync(
            new ThesisId(thesisId),
            new UpdateThesisRequest(request.Title, request.Body, request.Status, request.InstrumentId),
            cancellationToken);

        return thesis is null ? NotFound() : Ok(thesis);
    }
}

public sealed record CreateThesisDto(string Title, string Body, ThesisStatus Status, Guid? InstrumentId);
public sealed record UpdateThesisDto(string Title, string Body, ThesisStatus Status, Guid? InstrumentId);

