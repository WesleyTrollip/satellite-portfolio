using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;
using System.Text.Json;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController(
    TradeService service,
    IInstrumentRepository instruments,
    ICorrectionReasonLookupRepository correctionReasons) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TradeListItemDto>>> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? instrumentId,
        CancellationToken cancellationToken)
    {
        var trades = await service.ListAsync(from, to, instrumentId.HasValue ? new InstrumentId(instrumentId.Value) : null, cancellationToken);
        var allInstruments = await instruments.ListAsync(cancellationToken);
        var allReasons = await correctionReasons.ListAsync(null, true, 0, 1000, cancellationToken);
        var instrumentMap = allInstruments.ToDictionary(x => x.Id, x => x);
        var reasonMap = allReasons.ToDictionary(x => x.Id, x => x.Name);
        var response = trades.Select(trade =>
        {
            instrumentMap.TryGetValue(trade.InstrumentId, out var instrument);
            var correctionReasonId = trade.CorrectionReasonLookupId?.Value;
            return new TradeListItemDto(
                trade.Id.Value,
                trade.InstrumentId.Value,
                instrument?.Symbol ?? "UNKNOWN",
                instrument is null ? "UNKNOWN" : string.IsNullOrWhiteSpace(instrument.Name) ? instrument.Symbol : $"{instrument.Symbol} - {instrument.Name}",
                trade.Side,
                trade.Quantity,
                trade.PriceAmount,
                trade.FeesAmount,
                trade.CostBasisMode,
                trade.CustomTotalCost,
                trade.ExecutedAt,
                trade.Notes,
                trade.CorrectionGroupId?.Value,
                trade.CorrectedByTradeId?.Value,
                trade.IsCorrectionReversal,
                correctionReasonId,
                correctionReasonId.HasValue && reasonMap.TryGetValue(new CorrectionReasonLookupId(correctionReasonId.Value), out var name) ? name : null);
        }).ToList();
        return Ok(response);
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
        #region agent log
        DebugRuntimeLogger.Log(
            hypothesisId: "H1",
            location: "TradesController.Create",
            message: "Incoming create trade request datetime",
            data: new
            {
                executedAt = request.ExecutedAt,
                executedAtKind = request.ExecutedAt.Kind.ToString(),
                instrumentId = request.InstrumentId,
                side = request.Side.ToString()
            });
        #endregion

        var trade = await service.CreateAsync(
            new CreateTradeRequest(
                new InstrumentId(request.InstrumentId),
                request.Side,
                request.Quantity,
                request.PriceAmount,
                request.FeesAmount,
                request.CostBasisMode,
                request.CustomTotalCost,
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
                request.CostBasisMode,
                request.CustomTotalCost,
                request.ExecutedAt,
                request.Notes,
                request.CorrectionReasonLookupId),
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
    CostBasisMode? CostBasisMode,
    decimal? CustomTotalCost,
    DateTime ExecutedAt,
    string? Notes);

public sealed record CreateTradeCorrectionDto(
    decimal Quantity,
    decimal PriceAmount,
    decimal FeesAmount,
    CostBasisMode? CostBasisMode,
    decimal? CustomTotalCost,
    DateTime ExecutedAt,
    string? Notes,
    Guid? CorrectionReasonLookupId);

public sealed record TradeListItemDto(
    Guid Id,
    Guid InstrumentId,
    string InstrumentSymbol,
    string InstrumentLabel,
    TradeSide Side,
    decimal Quantity,
    decimal PriceAmount,
    decimal FeesAmount,
    CostBasisMode? CostBasisMode,
    decimal? CustomTotalCost,
    DateTime ExecutedAt,
    string? Notes,
    Guid? CorrectionGroupId,
    Guid? CorrectedByTradeId,
    bool IsCorrectionReversal,
    Guid? CorrectionReasonLookupId,
    string? CorrectionReasonName);

internal static class DebugRuntimeLogger
{
    private const string LogPath = "debug-fbcfbe.log";

    public static void Log(string hypothesisId, string location, string message, object data)
    {
        var payload = new
        {
            sessionId = "fbcfbe",
            runId = Guid.NewGuid().ToString("N"),
            hypothesisId,
            location,
            message,
            data,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var line = JsonSerializer.Serialize(payload);
        File.AppendAllText(LogPath, line + Environment.NewLine);
    }
}

