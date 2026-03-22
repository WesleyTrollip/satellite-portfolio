using SatellitePortfolio.Domain;
using System.Text.Json;

namespace SatellitePortfolio.Application;

public sealed record CreateInstrumentRequest(string Symbol, string? Name, Guid? SectorLookupId, string Currency);
public sealed record UpdateInstrumentRequest(string Symbol, string? Name, Guid? SectorLookupId, string Currency);

public sealed record CreateTradeRequest(
    InstrumentId InstrumentId,
    TradeSide Side,
    decimal Quantity,
    decimal PriceAmount,
    decimal FeesAmount,
    CostBasisMode? CostBasisMode,
    decimal? CustomTotalCost,
    DateTime ExecutedAt,
    string? Notes);

public sealed record CreateTradeCorrectionRequest(
    decimal Quantity,
    decimal PriceAmount,
    decimal FeesAmount,
    CostBasisMode? CostBasisMode,
    decimal? CustomTotalCost,
    DateTime ExecutedAt,
    string? Notes,
    Guid? CorrectionReasonLookupId);

public sealed record CreateCashEntryRequest(
    CashEntryType Type,
    decimal Amount,
    DateTime OccurredAt,
    string? Notes);

public sealed record CreateCashCorrectionRequest(
    CashEntryType Type,
    decimal Amount,
    DateTime OccurredAt,
    string? Notes,
    string? Reason);

public sealed class InstrumentService(
    IInstrumentRepository instruments,
    ISectorLookupRepository sectorLookups,
    ITradeRepository trades,
    IPriceSnapshotRepository priceSnapshots,
    IThesisRepository theses,
    IJournalLinkRepository journalLinks,
    IPortfolioUnitOfWork unitOfWork)
{
    private static readonly PortfolioId LocalPortfolioId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken)
        => instruments.ListAsync(cancellationToken);

    public Task<Instrument?> GetByIdAsync(InstrumentId instrumentId, CancellationToken cancellationToken)
        => instruments.GetByIdAsync(instrumentId, cancellationToken);

    public async Task<Instrument> CreateAsync(CreateInstrumentRequest request, CancellationToken cancellationToken)
    {
        var sector = await ResolveSectorLookupAsync(
            request.SectorLookupId.HasValue ? new SectorLookupId(request.SectorLookupId.Value) : null,
            cancellationToken);

        var instrument = new Instrument
        {
            Id = new InstrumentId(Guid.NewGuid()),
            PortfolioId = LocalPortfolioId,
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            Name = request.Name?.Trim(),
            Sector = sector?.Name,
            SectorLookupId = request.SectorLookupId.HasValue ? new SectorLookupId(request.SectorLookupId.Value) : null,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency.Trim().ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        await instruments.AddAsync(instrument, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return instrument;
    }

    public async Task<Instrument?> UpdateAsync(InstrumentId instrumentId, UpdateInstrumentRequest request, CancellationToken cancellationToken)
    {
        var existing = await instruments.GetByIdAsync(instrumentId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var sector = await ResolveSectorLookupAsync(
            request.SectorLookupId.HasValue ? new SectorLookupId(request.SectorLookupId.Value) : null,
            cancellationToken);

        var updated = new Instrument
        {
            Id = existing.Id,
            PortfolioId = existing.PortfolioId,
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            Name = request.Name?.Trim(),
            Sector = sector?.Name,
            SectorLookupId = request.SectorLookupId.HasValue ? new SectorLookupId(request.SectorLookupId.Value) : null,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency.Trim().ToUpperInvariant(),
            CreatedAt = existing.CreatedAt
        };

        await instruments.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<bool> DeleteAsync(InstrumentId instrumentId, CancellationToken cancellationToken)
    {
        var existing = await instruments.GetByIdAsync(instrumentId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        if ((await trades.ListAllAsync(cancellationToken)).Any(x => x.InstrumentId == instrumentId))
        {
            throw new InvalidOperationException("Cannot delete instrument because it is referenced by trades.");
        }

        if ((await priceSnapshots.ListAllAsync(cancellationToken)).Any(x => x.InstrumentId == instrumentId))
        {
            throw new InvalidOperationException("Cannot delete instrument because it is referenced by price snapshots.");
        }

        if ((await theses.ListAsync(cancellationToken)).Any(x => x.InstrumentId == instrumentId))
        {
            throw new InvalidOperationException("Cannot delete instrument because it is referenced by theses.");
        }

        if (await journalLinks.CountByInstrumentIdAsync(instrumentId, cancellationToken) > 0)
        {
            throw new InvalidOperationException("Cannot delete instrument because it is referenced by journal entries.");
        }

        await instruments.DeleteAsync(existing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<SectorLookup?> ResolveSectorLookupAsync(SectorLookupId? sectorLookupId, CancellationToken cancellationToken)
    {
        if (!sectorLookupId.HasValue)
        {
            return null;
        }

        var sector = await sectorLookups.GetByIdAsync(sectorLookupId.Value, cancellationToken);
        if (sector is null || !sector.IsActive)
        {
            throw new InvalidOperationException($"Sector '{sectorLookupId.Value.Value}' is invalid or inactive.");
        }

        return sector;
    }
}

public sealed class TradeService(
    ITradeRepository trades,
    ICashLedgerRepository cashEntries,
    IPriceSnapshotRepository prices,
    ICorrectionReasonLookupRepository correctionReasons,
    IPortfolioUnitOfWork unitOfWork,
    IHoldingsCalculator holdingsCalculator)
{
    private static readonly PortfolioId LocalPortfolioId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public Task<IReadOnlyCollection<Trade>> ListAsync(DateTime? from, DateTime? to, InstrumentId? instrumentId, CancellationToken cancellationToken)
        => trades.ListAsync(from, to, instrumentId, cancellationToken);

    public Task<Trade?> GetByIdAsync(TradeId tradeId, CancellationToken cancellationToken)
        => trades.GetByIdAsync(tradeId, cancellationToken);

    public async Task<Trade> CreateAsync(CreateTradeRequest request, CancellationToken cancellationToken)
    {
        #region agent log
        DebugRuntimeLogger.Log(
            hypothesisId: "H2",
            location: "TradeService.CreateAsync",
            message: "Trade service received datetime",
            data: new
            {
                executedAt = request.ExecutedAt,
                executedAtKind = request.ExecutedAt.Kind.ToString(),
                instrumentId = request.InstrumentId.Value,
                side = request.Side.ToString()
            });
        #endregion

        var normalizedBasis = NormalizeCostBasis(request.Side, request.CostBasisMode, request.CustomTotalCost);
        ValidateTradeRequest(request.Side, request.Quantity, request.PriceAmount, request.FeesAmount);
        var executedAtUtc = NormalizeToUtc(request.ExecutedAt);
        await EnsureSellDoesNotExceedHoldings(
            new CreateTradeRequest(
                request.InstrumentId,
                request.Side,
                request.Quantity,
                request.PriceAmount,
                request.FeesAmount,
                normalizedBasis.Mode,
                normalizedBasis.CustomTotalCost,
                executedAtUtc,
                request.Notes),
            cancellationToken);

        var trade = new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = LocalPortfolioId,
            InstrumentId = request.InstrumentId,
            Side = request.Side,
            Quantity = request.Quantity,
            PriceAmount = request.PriceAmount,
            FeesAmount = request.FeesAmount,
            CostBasisMode = normalizedBasis.Mode,
            CustomTotalCost = normalizedBasis.CustomTotalCost,
            ExecutedAt = executedAtUtc,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await trades.AddRangeAsync([trade], cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return trade;
    }

    public async Task<IReadOnlyCollection<Trade>> CorrectAsync(
        TradeId tradeId,
        CreateTradeCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        var executedAtUtc = NormalizeToUtc(request.ExecutedAt);

        var original = await trades.GetByIdAsync(tradeId, cancellationToken)
                       ?? throw new InvalidOperationException($"Trade '{tradeId.Value}' was not found.");
        var normalizedBasis = NormalizeCostBasis(original.Side, request.CostBasisMode, request.CustomTotalCost);
        ValidateTradeRequest(original.Side, request.Quantity, request.PriceAmount, request.FeesAmount);

        var replacementDraft = new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = original.PortfolioId,
            InstrumentId = original.InstrumentId,
            Side = original.Side,
            Quantity = request.Quantity,
            PriceAmount = request.PriceAmount,
            FeesAmount = request.FeesAmount,
            CostBasisMode = normalizedBasis.Mode,
            CustomTotalCost = normalizedBasis.CustomTotalCost,
            ExecutedAt = executedAtUtc,
            Notes = request.Notes?.Trim(),
            CorrectionReasonLookupId = request.CorrectionReasonLookupId.HasValue
                ? new CorrectionReasonLookupId(request.CorrectionReasonLookupId.Value)
                : null,
            CreatedAt = DateTime.UtcNow
        };

        await ValidateCorrectionReasonAsync(replacementDraft.CorrectionReasonLookupId, cancellationToken);

        await EnsureSellDoesNotExceedHoldings(
            new CreateTradeRequest(
                replacementDraft.InstrumentId,
                replacementDraft.Side,
                replacementDraft.Quantity,
                replacementDraft.PriceAmount,
                replacementDraft.FeesAmount,
                replacementDraft.CostBasisMode,
                replacementDraft.CustomTotalCost,
                replacementDraft.ExecutedAt,
                replacementDraft.Notes),
            cancellationToken);

        var (reversal, replacement) = CorrectionFactory.CreateTradeCorrection(original, replacementDraft, DateTime.UtcNow);
        await trades.AddRangeAsync([reversal, replacement], cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return [reversal, replacement];
    }

    private async Task EnsureSellDoesNotExceedHoldings(CreateTradeRequest request, CancellationToken cancellationToken)
    {
        if (request.Side != TradeSide.Sell)
        {
            return;
        }

        var allTrades = await trades.ListAllAsync(cancellationToken);
        var allCash = await cashEntries.ListAllAsync(cancellationToken);
        var allPrices = await prices.ListAllAsync(cancellationToken);

        var snapshot = holdingsCalculator.CalculateSnapshot(allTrades, allCash, allPrices, request.ExecutedAt);
        var currentHolding = snapshot.Holdings.SingleOrDefault(h => h.InstrumentId == request.InstrumentId);
        var quantityAvailable = currentHolding?.Quantity ?? 0m;

        if (quantityAvailable < request.Quantity)
        {
            throw new InvalidOperationException("Cannot sell more quantity than currently held.");
        }
    }

    private static void ValidateTradeRequest(TradeSide side, decimal quantity, decimal priceAmount, decimal feesAmount)
    {
        if (quantity <= 0m)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        if (priceAmount < 0m)
        {
            throw new InvalidOperationException("Price cannot be negative.");
        }

        if (feesAmount < 0m)
        {
            throw new InvalidOperationException("Fees cannot be negative.");
        }

        if (side == TradeSide.NonCashAcquisition && feesAmount != 0m)
        {
            throw new InvalidOperationException("Fees must be zero for non-cash acquisitions.");
        }
    }

    private static DateTime NormalizeToUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };

    private static (CostBasisMode? Mode, decimal? CustomTotalCost) NormalizeCostBasis(
        TradeSide side,
        CostBasisMode? costBasisMode,
        decimal? customTotalCost)
    {
        if (side != TradeSide.NonCashAcquisition)
        {
            if (costBasisMode.HasValue || customTotalCost.HasValue)
            {
                throw new InvalidOperationException("Cost basis fields are only supported for non-cash acquisitions.");
            }

            return (null, null);
        }

        var mode = costBasisMode ?? CostBasisMode.Zero;
        if (mode == CostBasisMode.Zero)
        {
            return (CostBasisMode.Zero, null);
        }

        if (!customTotalCost.HasValue)
        {
            throw new InvalidOperationException("Custom total cost is required when cost basis mode is Custom.");
        }

        if (customTotalCost.Value < 0m)
        {
            throw new InvalidOperationException("Custom total cost cannot be negative.");
        }

        return (CostBasisMode.Custom, customTotalCost.Value);
    }

    private async Task ValidateCorrectionReasonAsync(CorrectionReasonLookupId? correctionReasonLookupId, CancellationToken cancellationToken)
    {
        if (!correctionReasonLookupId.HasValue)
        {
            return;
        }

        var reason = await correctionReasons.GetByIdAsync(correctionReasonLookupId.Value, cancellationToken);
        if (reason is null || !reason.IsActive)
        {
            throw new InvalidOperationException($"Correction reason '{correctionReasonLookupId.Value.Value}' is invalid or inactive.");
        }
    }
}

public sealed class CashLedgerService(
    ICashLedgerRepository cashEntries,
    IPortfolioUnitOfWork unitOfWork)
{
    private static readonly PortfolioId LocalPortfolioId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public Task<IReadOnlyCollection<CashLedgerEntry>> ListAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
        => cashEntries.ListAsync(from, to, cancellationToken);

    public Task<CashLedgerEntry?> GetByIdAsync(CashEntryId cashEntryId, CancellationToken cancellationToken)
        => cashEntries.GetByIdAsync(cashEntryId, cancellationToken);

    public async Task<CashLedgerEntry> CreateAsync(CreateCashEntryRequest request, CancellationToken cancellationToken)
    {
        ValidateAmount(request.Amount);

        var entry = new CashLedgerEntry
        {
            Id = new CashEntryId(Guid.NewGuid()),
            PortfolioId = LocalPortfolioId,
            Type = request.Type,
            Amount = request.Amount,
            OccurredAt = request.OccurredAt,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await cashEntries.AddRangeAsync([entry], cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<IReadOnlyCollection<CashLedgerEntry>> CorrectAsync(
        CashEntryId cashEntryId,
        CreateCashCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAmount(request.Amount);
        var original = await cashEntries.GetByIdAsync(cashEntryId, cancellationToken)
                       ?? throw new InvalidOperationException($"Cash entry '{cashEntryId.Value}' was not found.");

        var replacementDraft = new CashLedgerEntry
        {
            Id = new CashEntryId(Guid.NewGuid()),
            PortfolioId = original.PortfolioId,
            Type = request.Type,
            Amount = request.Amount,
            OccurredAt = request.OccurredAt,
            Notes = BuildCorrectionNotes(request.Notes, request.Reason),
            CreatedAt = DateTime.UtcNow
        };

        var (reversal, replacement) = CorrectionFactory.CreateCashCorrection(original, replacementDraft, DateTime.UtcNow);
        await cashEntries.AddRangeAsync([reversal, replacement], cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return [reversal, replacement];
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount == 0m)
        {
            throw new InvalidOperationException("Cash amount cannot be zero.");
        }
    }

    private static string? BuildCorrectionNotes(string? notes, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return notes;
        }

        if (string.IsNullOrWhiteSpace(notes))
        {
            return $"Correction reason: {reason}";
        }

        return $"{notes} | Correction reason: {reason}";
    }
}

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

