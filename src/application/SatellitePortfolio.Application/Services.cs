using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application;

public sealed record CreateInstrumentRequest(string Symbol, string? Name, string? Sector, string Currency);
public sealed record UpdateInstrumentRequest(string Symbol, string? Name, string? Sector, string Currency);

public sealed record CreateTradeRequest(
    InstrumentId InstrumentId,
    TradeSide Side,
    decimal Quantity,
    decimal PriceAmount,
    decimal FeesAmount,
    DateTime ExecutedAt,
    string? Notes);

public sealed record CreateTradeCorrectionRequest(
    decimal Quantity,
    decimal PriceAmount,
    decimal FeesAmount,
    DateTime ExecutedAt,
    string? Notes,
    string? Reason);

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
    IPortfolioUnitOfWork unitOfWork)
{
    private static readonly PortfolioId LocalPortfolioId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken)
        => instruments.ListAsync(cancellationToken);

    public Task<Instrument?> GetByIdAsync(InstrumentId instrumentId, CancellationToken cancellationToken)
        => instruments.GetByIdAsync(instrumentId, cancellationToken);

    public async Task<Instrument> CreateAsync(CreateInstrumentRequest request, CancellationToken cancellationToken)
    {
        var instrument = new Instrument
        {
            Id = new InstrumentId(Guid.NewGuid()),
            PortfolioId = LocalPortfolioId,
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            Name = request.Name?.Trim(),
            Sector = request.Sector?.Trim(),
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

        var updated = new Instrument
        {
            Id = existing.Id,
            PortfolioId = existing.PortfolioId,
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            Name = request.Name?.Trim(),
            Sector = request.Sector?.Trim(),
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency.Trim().ToUpperInvariant(),
            CreatedAt = existing.CreatedAt
        };

        await instruments.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return updated;
    }
}

public sealed class TradeService(
    ITradeRepository trades,
    ICashLedgerRepository cashEntries,
    IPriceSnapshotRepository prices,
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
        ValidateTradeRequest(request.Quantity, request.PriceAmount, request.FeesAmount);
        await EnsureSellDoesNotExceedHoldings(request, cancellationToken);

        var trade = new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = LocalPortfolioId,
            InstrumentId = request.InstrumentId,
            Side = request.Side,
            Quantity = request.Quantity,
            PriceAmount = request.PriceAmount,
            FeesAmount = request.FeesAmount,
            ExecutedAt = request.ExecutedAt,
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
        ValidateTradeRequest(request.Quantity, request.PriceAmount, request.FeesAmount);

        var original = await trades.GetByIdAsync(tradeId, cancellationToken)
                       ?? throw new InvalidOperationException($"Trade '{tradeId.Value}' was not found.");

        var replacementDraft = new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = original.PortfolioId,
            InstrumentId = original.InstrumentId,
            Side = original.Side,
            Quantity = request.Quantity,
            PriceAmount = request.PriceAmount,
            FeesAmount = request.FeesAmount,
            ExecutedAt = request.ExecutedAt,
            Notes = BuildCorrectionNotes(request.Notes, request.Reason),
            CreatedAt = DateTime.UtcNow
        };

        await EnsureSellDoesNotExceedHoldings(
            new CreateTradeRequest(
                replacementDraft.InstrumentId,
                replacementDraft.Side,
                replacementDraft.Quantity,
                replacementDraft.PriceAmount,
                replacementDraft.FeesAmount,
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

    private static void ValidateTradeRequest(decimal quantity, decimal priceAmount, decimal feesAmount)
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

