using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application.Tests;

public class TradeAndCashServicesTests
{
    [Fact]
    public async Task CreateTrade_And_CreateCashEntry_PersistSuccessfully()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var trades = new InMemoryTradeRepository();
        var cash = new InMemoryCashLedgerRepository();
        var prices = new InMemoryPriceSnapshotRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var calculator = new HoldingsCalculator();

        var tradeService = new TradeService(trades, cash, prices, new InMemoryCorrectionReasonLookupRepository(), unitOfWork, calculator);
        var cashService = new CashLedgerService(cash, unitOfWork);

        var entry = await cashService.CreateAsync(
            new CreateCashEntryRequest(CashEntryType.Deposit, 10_000m, DateTime.UtcNow, "seed"),
            CancellationToken.None);

        var trade = await tradeService.CreateAsync(
            new CreateTradeRequest(instrumentId, TradeSide.Buy, 10m, 100m, 5m, null, null, DateTime.UtcNow, "buy"),
            CancellationToken.None);

        Assert.Single(cash.Items);
        Assert.Single(trades.Items);
        Assert.Equal(10_000m, entry.Amount);
        Assert.Equal(10m, trade.Quantity);
    }

    [Fact]
    public async Task TradeCorrection_CreatesReversalAndReplacement()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var trades = new InMemoryTradeRepository();
        var cash = new InMemoryCashLedgerRepository();
        var prices = new InMemoryPriceSnapshotRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var calculator = new HoldingsCalculator();
        var tradeService = new TradeService(trades, cash, prices, new InMemoryCorrectionReasonLookupRepository(), unitOfWork, calculator);

        var original = await tradeService.CreateAsync(
            new CreateTradeRequest(instrumentId, TradeSide.Buy, 5m, 100m, 1m, null, null, DateTime.UtcNow, "original"),
            CancellationToken.None);

        var corrected = await tradeService.CorrectAsync(
            original.Id,
            new CreateTradeCorrectionRequest(5m, 95m, 1m, null, null, DateTime.UtcNow, "replacement", null),
            CancellationToken.None);

        Assert.Equal(3, trades.Items.Count);
        Assert.Equal(2, corrected.Count);
        Assert.Contains(corrected, x => x.IsCorrectionReversal);
        Assert.Contains(corrected, x => !x.IsCorrectionReversal);
    }

    [Fact]
    public async Task SellingMoreThanHoldings_IsRejected()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var trades = new InMemoryTradeRepository();
        var cash = new InMemoryCashLedgerRepository();
        var prices = new InMemoryPriceSnapshotRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var calculator = new HoldingsCalculator();
        var tradeService = new TradeService(trades, cash, prices, new InMemoryCorrectionReasonLookupRepository(), unitOfWork, calculator);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tradeService.CreateAsync(
                new CreateTradeRequest(instrumentId, TradeSide.Sell, 1m, 100m, 0m, null, null, DateTime.UtcNow, "invalid"),
                CancellationToken.None));
    }

    [Fact]
    public async Task TradeHistory_IncludesCorrectionEntries()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var trades = new InMemoryTradeRepository();
        var cash = new InMemoryCashLedgerRepository();
        var prices = new InMemoryPriceSnapshotRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var calculator = new HoldingsCalculator();
        var tradeService = new TradeService(trades, cash, prices, new InMemoryCorrectionReasonLookupRepository(), unitOfWork, calculator);

        var original = await tradeService.CreateAsync(
            new CreateTradeRequest(instrumentId, TradeSide.Buy, 2m, 100m, 0m, null, null, DateTime.UtcNow.AddMinutes(-2), "original"),
            CancellationToken.None);

        await tradeService.CorrectAsync(
            original.Id,
            new CreateTradeCorrectionRequest(2m, 101m, 0m, null, null, DateTime.UtcNow.AddMinutes(-1), "replacement", null),
            CancellationToken.None);

        var history = await tradeService.ListAsync(null, null, null, CancellationToken.None);
        Assert.Equal(3, history.Count);
        Assert.Contains(history, x => x.Id == original.Id);
        Assert.Equal(2, history.Count(x => x.CorrectionGroupId.HasValue));
    }

    [Fact]
    public async Task CreateNonCashAcquisition_WithZeroBasis_DoesNotChangeCashBalance()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var trades = new InMemoryTradeRepository();
        var cash = new InMemoryCashLedgerRepository();
        var prices = new InMemoryPriceSnapshotRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var calculator = new HoldingsCalculator();
        var tradeService = new TradeService(trades, cash, prices, new InMemoryCorrectionReasonLookupRepository(), unitOfWork, calculator);

        await tradeService.CreateAsync(
            new CreateTradeRequest(instrumentId, TradeSide.NonCashAcquisition, 10m, 0m, 0m, CostBasisMode.Zero, null, DateTime.UtcNow, "gifted"),
            CancellationToken.None);

        var snapshot = calculator.CalculateSnapshot(trades.Items, cash.Items, prices.Items, DateTime.UtcNow.AddMinutes(1));
        var holding = Assert.Single(snapshot.Holdings);
        Assert.Equal(10m, holding.Quantity);
        Assert.Equal(0m, snapshot.Totals.CashBalance.Amount);
    }

    [Fact]
    public async Task CreateNonCashAcquisition_WithCustomBasis_PersistsBasisFields()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var trades = new InMemoryTradeRepository();
        var cash = new InMemoryCashLedgerRepository();
        var prices = new InMemoryPriceSnapshotRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var calculator = new HoldingsCalculator();
        var tradeService = new TradeService(trades, cash, prices, new InMemoryCorrectionReasonLookupRepository(), unitOfWork, calculator);

        var created = await tradeService.CreateAsync(
            new CreateTradeRequest(instrumentId, TradeSide.NonCashAcquisition, 10m, 0m, 0m, CostBasisMode.Custom, 120m, DateTime.UtcNow, "grant"),
            CancellationToken.None);

        Assert.Equal(CostBasisMode.Custom, created.CostBasisMode);
        Assert.Equal(120m, created.CustomTotalCost);
    }

    [Fact]
    public async Task CustomCostBasis_OnStandardBuy_IsRejected()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var trades = new InMemoryTradeRepository();
        var cash = new InMemoryCashLedgerRepository();
        var prices = new InMemoryPriceSnapshotRepository();
        var unitOfWork = new InMemoryUnitOfWork();
        var calculator = new HoldingsCalculator();
        var tradeService = new TradeService(trades, cash, prices, new InMemoryCorrectionReasonLookupRepository(), unitOfWork, calculator);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tradeService.CreateAsync(
                new CreateTradeRequest(instrumentId, TradeSide.Buy, 1m, 100m, 0m, CostBasisMode.Custom, 100m, DateTime.UtcNow, "invalid"),
                CancellationToken.None));
    }
}

internal sealed class InMemoryTradeRepository : ITradeRepository
{
    public List<Trade> Items { get; } = [];

    public Task<IReadOnlyCollection<Trade>> ListAsync(DateTime? from, DateTime? to, InstrumentId? instrumentId, CancellationToken cancellationToken)
    {
        IEnumerable<Trade> query = Items;
        if (from.HasValue) query = query.Where(x => x.ExecutedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.ExecutedAt <= to.Value);
        if (instrumentId.HasValue) query = query.Where(x => x.InstrumentId == instrumentId.Value);
        return Task.FromResult<IReadOnlyCollection<Trade>>(query.ToList());
    }

    public Task<IReadOnlyCollection<Trade>> ListAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<Trade>>(Items.ToList());

    public Task<Trade?> GetByIdAsync(TradeId tradeId, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == tradeId));

    public Task AddRangeAsync(IEnumerable<Trade> trades, CancellationToken cancellationToken)
    {
        Items.AddRange(trades);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryCashLedgerRepository : ICashLedgerRepository
{
    public List<CashLedgerEntry> Items { get; } = [];

    public Task<IReadOnlyCollection<CashLedgerEntry>> ListAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        IEnumerable<CashLedgerEntry> query = Items;
        if (from.HasValue) query = query.Where(x => x.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredAt <= to.Value);
        return Task.FromResult<IReadOnlyCollection<CashLedgerEntry>>(query.ToList());
    }

    public Task<IReadOnlyCollection<CashLedgerEntry>> ListAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<CashLedgerEntry>>(Items.ToList());

    public Task<CashLedgerEntry?> GetByIdAsync(CashEntryId cashEntryId, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == cashEntryId));

    public Task AddRangeAsync(IEnumerable<CashLedgerEntry> entries, CancellationToken cancellationToken)
    {
        Items.AddRange(entries);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPriceSnapshotRepository : IPriceSnapshotRepository
{
    public List<PriceSnapshot> Items { get; } = [];

    public Task<IReadOnlyCollection<PriceSnapshot>> ListAsync(
        InstrumentId? instrumentId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        IEnumerable<PriceSnapshot> query = Items;
        if (instrumentId.HasValue) query = query.Where(x => x.InstrumentId == instrumentId.Value);
        if (from.HasValue) query = query.Where(x => x.Date >= from.Value);
        if (to.HasValue) query = query.Where(x => x.Date <= to.Value);
        return Task.FromResult<IReadOnlyCollection<PriceSnapshot>>(query.ToList());
    }

    public Task<IReadOnlyCollection<PriceSnapshot>> ListAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<PriceSnapshot>>(Items.ToList());

    public Task<PriceSnapshot?> GetByInstrumentAndDateAsync(InstrumentId instrumentId, DateOnly date, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.InstrumentId == instrumentId && x.Date == date));

    public Task AddAsync(PriceSnapshot snapshot, CancellationToken cancellationToken)
    {
        Items.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PriceSnapshot snapshot, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(x => x.Id == snapshot.Id);
        if (index >= 0)
        {
            Items[index] = snapshot;
        }

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryUnitOfWork : IPortfolioUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal sealed class InMemoryAlertEventRepository : IAlertEventRepository
{
    public List<AlertEvent> Items { get; } = [];

    public Task<IReadOnlyCollection<AlertEvent>> ListAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        IEnumerable<AlertEvent> query = Items;
        if (from.HasValue) query = query.Where(x => x.TriggeredAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.TriggeredAt <= to.Value);
        return Task.FromResult<IReadOnlyCollection<AlertEvent>>(query.ToList());
    }

    public Task<IReadOnlyCollection<AlertEvent>> ListCurrentAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<AlertEvent>>(Items.ToList());

    public Task AddRangeAsync(IEnumerable<AlertEvent> alertEvents, CancellationToken cancellationToken)
    {
        Items.AddRange(alertEvents);
        return Task.CompletedTask;
    }
}

internal sealed class InMemorySectorLookupRepository : ISectorLookupRepository
{
    public List<SectorLookup> Items { get; } = [];

    public Task<IReadOnlyCollection<SectorLookup>> ListAsync(string? search, bool? isActive, int skip, int take, CancellationToken cancellationToken)
    {
        IEnumerable<SectorLookup> query = Items;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.Code.Contains(normalized, StringComparison.OrdinalIgnoreCase) || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return Task.FromResult<IReadOnlyCollection<SectorLookup>>(query.Skip(skip).Take(take).ToList());
    }

    public Task<SectorLookup?> GetByIdAsync(SectorLookupId sectorLookupId, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == sectorLookupId));

    public Task<SectorLookup?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(SectorLookup sectorLookup, CancellationToken cancellationToken)
    {
        Items.Add(sectorLookup);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SectorLookup sectorLookup, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(x => x.Id == sectorLookup.Id);
        if (index >= 0)
        {
            Items[index] = sectorLookup;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(SectorLookup sectorLookup, CancellationToken cancellationToken)
    {
        Items.RemoveAll(x => x.Id == sectorLookup.Id);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPriceSourceLookupRepository : IPriceSourceLookupRepository
{
    public List<PriceSourceLookup> Items { get; } = [];

    public Task<IReadOnlyCollection<PriceSourceLookup>> ListAsync(string? search, bool? isActive, int skip, int take, CancellationToken cancellationToken)
    {
        IEnumerable<PriceSourceLookup> query = Items;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.Code.Contains(normalized, StringComparison.OrdinalIgnoreCase) || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return Task.FromResult<IReadOnlyCollection<PriceSourceLookup>>(query.Skip(skip).Take(take).ToList());
    }

    public Task<PriceSourceLookup?> GetByIdAsync(PriceSourceLookupId priceSourceLookupId, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == priceSourceLookupId));

    public Task<PriceSourceLookup?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(PriceSourceLookup priceSourceLookup, CancellationToken cancellationToken)
    {
        Items.Add(priceSourceLookup);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PriceSourceLookup priceSourceLookup, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(x => x.Id == priceSourceLookup.Id);
        if (index >= 0)
        {
            Items[index] = priceSourceLookup;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(PriceSourceLookup priceSourceLookup, CancellationToken cancellationToken)
    {
        Items.RemoveAll(x => x.Id == priceSourceLookup.Id);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryCorrectionReasonLookupRepository : ICorrectionReasonLookupRepository
{
    public List<CorrectionReasonLookup> Items { get; } = [];

    public Task<IReadOnlyCollection<CorrectionReasonLookup>> ListAsync(string? search, bool? isActive, int skip, int take, CancellationToken cancellationToken)
    {
        IEnumerable<CorrectionReasonLookup> query = Items;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.Code.Contains(normalized, StringComparison.OrdinalIgnoreCase) || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return Task.FromResult<IReadOnlyCollection<CorrectionReasonLookup>>(query.Skip(skip).Take(take).ToList());
    }

    public Task<CorrectionReasonLookup?> GetByIdAsync(CorrectionReasonLookupId correctionReasonLookupId, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == correctionReasonLookupId));

    public Task<CorrectionReasonLookup?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(CorrectionReasonLookup correctionReasonLookup, CancellationToken cancellationToken)
    {
        Items.Add(correctionReasonLookup);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CorrectionReasonLookup correctionReasonLookup, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(x => x.Id == correctionReasonLookup.Id);
        if (index >= 0)
        {
            Items[index] = correctionReasonLookup;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(CorrectionReasonLookup correctionReasonLookup, CancellationToken cancellationToken)
    {
        Items.RemoveAll(x => x.Id == correctionReasonLookup.Id);
        return Task.CompletedTask;
    }
}
