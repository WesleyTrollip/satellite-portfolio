using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/lookups/sectors")]
public sealed class SectorsController(SectorLookupService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<LookupView>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(new LookupListRequest(search, isActive, skip, take), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LookupView>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(new SectorLookupId(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<LookupView>> Create([FromBody] UpsertLookupDto request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(new UpsertLookupRequest(request.Code, request.Name, request.IsActive), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LookupView>> Update(Guid id, [FromBody] UpsertLookupDto request, CancellationToken cancellationToken)
    {
        var updated = await service.UpdateAsync(new SectorLookupId(id), new UpsertLookupRequest(request.Code, request.Name, request.IsActive), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await service.DeleteAsync(new SectorLookupId(id), cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }
}

public sealed record UpsertLookupDto(string Code, string Name, bool IsActive = true);
