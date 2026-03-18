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

        var tradeService = new TradeService(trades, cash, prices, unitOfWork, calculator);
        var cashService = new CashLedgerService(cash, unitOfWork);

        var entry = await cashService.CreateAsync(
            new CreateCashEntryRequest(CashEntryType.Deposit, 10_000m, DateTime.UtcNow, "seed"),
            CancellationToken.None);

        var trade = await tradeService.CreateAsync(
            new CreateTradeRequest(instrumentId, TradeSide.Buy, 10m, 100m, 5m, DateTime.UtcNow, "buy"),
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
        var tradeService = new TradeService(trades, cash, prices, unitOfWork, calculator);

        var original = await tradeService.CreateAsync(
            new CreateTradeRequest(instrumentId, TradeSide.Buy, 5m, 100m, 1m, DateTime.UtcNow, "original"),
            CancellationToken.None);

        var corrected = await tradeService.CorrectAsync(
            original.Id,
            new CreateTradeCorrectionRequest(5m, 95m, 1m, DateTime.UtcNow, "replacement", "wrong price"),
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
        var tradeService = new TradeService(trades, cash, prices, unitOfWork, calculator);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tradeService.CreateAsync(
                new CreateTradeRequest(instrumentId, TradeSide.Sell, 1m, 100m, 0m, DateTime.UtcNow, "invalid"),
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

    public Task<IReadOnlyCollection<PriceSnapshot>> ListAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<PriceSnapshot>>(Items.ToList());
}

internal sealed class InMemoryUnitOfWork : IPortfolioUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
