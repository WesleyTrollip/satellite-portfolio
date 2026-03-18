using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController(TradeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Trade>>> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? instrumentId,
        CancellationToken cancellationToken)
    {
        var trades = await service.ListAsync(from, to, instrumentId.HasValue ? new InstrumentId(instrumentId.Value) : null, cancellationToken);
        return Ok(trades);
    }

    [HttpGet("{tradeId:guid}")]
    public async Task<ActionResult<Trade>> GetById(Guid tradeId, CancellationToken cancellationToken)
    {
        var trade = await service.GetByIdAsync(new TradeId(tradeId), cancellationToken);
        return trade is null ? NotFound() : Ok(trade);
    }

    [HttpPost]
    public async Task<ActionResult<Trade>> Create([FromBody] CreateTradeDto request, CancellationToken cancellationToken)
    {
        var trade = await service.CreateAsync(
            new CreateTradeRequest(
                new InstrumentId(request.InstrumentId),
                request.Side,
                request.Quantity,
                request.PriceAmount,
                request.FeesAmount,
                request.ExecutedAt,
                request.Notes),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { tradeId = trade.Id.Value }, trade);
    }

    [HttpPost("{tradeId:guid}/corrections")]
    public async Task<ActionResult<IReadOnlyCollection<Trade>>> Correct(
        Guid tradeId,
        [FromBody] CreateTradeCorrectionDto request,
        CancellationToken cancellationToken)
    {
        var correctedTrades = await service.CorrectAsync(
            new TradeId(tradeId),
            new CreateTradeCorrectionRequest(
                request.Quantity,
                request.PriceAmount,
                request.FeesAmount,
                request.ExecutedAt,
                request.Notes,
                request.Reason),
            cancellationToken);

        return Ok(correctedTrades);
    }
}

public sealed record CreateTradeDto(
    Guid InstrumentId,
    TradeSide Side,
    decimal Quantity,
    decimal PriceAmount,
    decimal FeesAmount,
    DateTime ExecutedAt,
    string? Notes);

public sealed record CreateTradeCorrectionDto(
    decimal Quantity,
    decimal PriceAmount,
    decimal FeesAmount,
    DateTime ExecutedAt,
    string? Notes,
    string? Reason);

