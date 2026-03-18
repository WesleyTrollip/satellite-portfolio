using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/journal")]
public sealed class JournalController(JournalService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<JournalEntryWithLinks>>> List(CancellationToken cancellationToken)
    {
        var entries = await service.ListAsync(cancellationToken);
        return Ok(entries);
    }

    [HttpGet("{journalEntryId:guid}")]
    public async Task<ActionResult<JournalEntryWithLinks>> GetById(Guid journalEntryId, CancellationToken cancellationToken)
    {
        var entry = await service.GetByIdAsync(new JournalEntryId(journalEntryId), cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost]
    public async Task<ActionResult<JournalEntryWithLinks>> Create([FromBody] CreateJournalEntryDto request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(
            new CreateJournalEntryRequest(
                request.OccurredAt,
                request.Title,
                request.Body,
                request.Tags,
                request.ThesisIds,
                request.InstrumentIds),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { journalEntryId = created.Entry.Id.Value }, created);
    }

    [HttpPut("{journalEntryId:guid}")]
    public async Task<ActionResult<JournalEntryWithLinks>> Update(
        Guid journalEntryId,
        [FromBody] UpdateJournalEntryDto request,
        CancellationToken cancellationToken)
    {
        var updated = await service.UpdateAsync(
            new JournalEntryId(journalEntryId),
            new UpdateJournalEntryRequest(
                request.OccurredAt,
                request.Title,
                request.Body,
                request.Tags,
                request.ThesisIds,
                request.InstrumentIds),
            cancellationToken);

        return updated is null ? NotFound() : Ok(updated);
    }
}

public sealed record CreateJournalEntryDto(
    DateTime OccurredAt,
    string Title,
    string Body,
    string? Tags,
    IReadOnlyCollection<Guid>? ThesisIds,
    IReadOnlyCollection<Guid>? InstrumentIds);

public sealed record UpdateJournalEntryDto(
    DateTime OccurredAt,
    string Title,
    string Body,
    string? Tags,
    IReadOnlyCollection<Guid>? ThesisIds,
    IReadOnlyCollection<Guid>? InstrumentIds);

