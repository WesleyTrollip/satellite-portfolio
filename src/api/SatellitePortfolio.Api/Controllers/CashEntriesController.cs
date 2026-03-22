using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/cash/entries")]
public sealed class CashEntriesController(CashLedgerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CashLedgerEntry>>> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var entries = await service.ListAsync(from, to, cancellationToken);
        return Ok(entries);
    }

    [HttpGet("{cashEntryId:guid}")]
    public async Task<ActionResult<CashLedgerEntry>> GetById(Guid cashEntryId, CancellationToken cancellationToken)
    {
        var entry = await service.GetByIdAsync(new CashEntryId(cashEntryId), cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost]
    public async Task<ActionResult<CashLedgerEntry>> Create([FromBody] CreateCashEntryDto request, CancellationToken cancellationToken)
    {
        var entry = await service.CreateAsync(
            new CreateCashEntryRequest(request.Type, request.Amount, request.OccurredAt, request.Notes),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { cashEntryId = entry.Id.Value }, entry);
    }

    [HttpPost("{cashEntryId:guid}/corrections")]
    public async Task<ActionResult<IReadOnlyCollection<CashLedgerEntry>>> Correct(
        Guid cashEntryId,
        [FromBody] CreateCashCorrectionDto request,
        CancellationToken cancellationToken)
    {
        var entries = await service.CorrectAsync(
            new CashEntryId(cashEntryId),
            new CreateCashCorrectionRequest(
                request.Type,
                request.Amount,
                request.OccurredAt,
                request.Notes,
                request.Reason),
            cancellationToken);

        return Ok(entries);
    }
}

public sealed record CreateCashEntryDto(
    CashEntryType Type,
    decimal Amount,
    DateTime OccurredAt,
    string? Notes);

public sealed record CreateCashCorrectionDto(
    CashEntryType Type,
    decimal Amount,
    DateTime OccurredAt,
    string? Notes,
    string? Reason);

