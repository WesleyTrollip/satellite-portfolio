using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application.Tests;

public class PortfolioQueryServiceTests
{
    [Fact]
    public async Task Overview_AggregatesTotalsAndSectorAllocations()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var instruments = new InMemoryInstrumentRepository();
        instruments.Items.Add(new Instrument
        {
            Id = instrumentId,
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Symbol = "MSFT",
            Sector = "Technology",
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow
        });

        var trades = new InMemoryTradeRepository();
        trades.Items.Add(new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            InstrumentId = instrumentId,
            Side = TradeSide.Buy,
            Quantity = 10m,
            PriceAmount = 100m,
            FeesAmount = 0m,
            ExecutedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        });

        var cash = new InMemoryCashLedgerRepository();
        cash.Items.Add(new CashLedgerEntry
        {
            Id = new CashEntryId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Type = CashEntryType.Deposit,
            Amount = 2_000m,
            OccurredAt = new DateTime(2026, 1, 31, 10, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        });

        var prices = new InMemoryPriceSnapshotRepository();
        prices.Items.Add(new PriceSnapshot
        {
            Id = new PriceSnapshotId(Guid.NewGuid()),
            InstrumentId = instrumentId,
            Date = new DateOnly(2026, 2, 1),
            ClosePriceAmount = 120m,
            PriceSourceLookupId = new PriceSourceLookupId(Guid.NewGuid()),
            CreatedAt = DateTime.UtcNow
        });

        var alerts = new InMemoryAlertEventRepository();
        var queryService = new PortfolioQueryService(instruments, new InMemorySectorLookupRepository(), trades, cash, prices, alerts, new HoldingsCalculator());
        var result = await queryService.GetOverviewAsync(new DateTime(2026, 2, 1, 20, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        Assert.Equal(1_200m, result.TotalMarketValue);
        Assert.Equal(1_000m, result.CashBalance);
        Assert.Equal(2_200m, result.PortfolioValue);
        Assert.Equal(200m, result.UnrealizedPnl);
        Assert.Single(result.SectorAllocations);
        Assert.Equal("Technology", result.SectorAllocations.First().Sector);
    }

    [Fact]
    public async Task Holdings_ExposeMissingPriceFlags()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var instruments = new InMemoryInstrumentRepository();
        instruments.Items.Add(new Instrument
        {
            Id = instrumentId,
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Symbol = "SAP",
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow
        });

        var trades = new InMemoryTradeRepository();
        trades.Items.Add(new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            InstrumentId = instrumentId,
            Side = TradeSide.Buy,
            Quantity = 1m,
            PriceAmount = 150m,
            FeesAmount = 0m,
            ExecutedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        var queryService = new PortfolioQueryService(
            instruments,
            new InMemorySectorLookupRepository(),
            trades,
            new InMemoryCashLedgerRepository(),
            new InMemoryPriceSnapshotRepository(),
            new InMemoryAlertEventRepository(),
            new HoldingsCalculator());

        var holdings = await queryService.GetHoldingsAsync(DateTime.UtcNow, CancellationToken.None);
        var holding = Assert.Single(holdings);

        Assert.True(holding.MissingPrice);
        Assert.NotNull(holding.MissingPriceExplanation);
    }

    [Fact]
    public async Task MonthlyState_UsesMonthEndAsOfAcrossBoundaries()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var instruments = new InMemoryInstrumentRepository();
        instruments.Items.Add(new Instrument
        {
            Id = instrumentId,
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Symbol = "NVDA",
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow
        });

        var trades = new InMemoryTradeRepository();
        trades.Items.Add(new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            InstrumentId = instrumentId,
            Side = TradeSide.Buy,
            Quantity = 5m,
            PriceAmount = 50m,
            FeesAmount = 0m,
            ExecutedAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        });

        var prices = new InMemoryPriceSnapshotRepository();
        prices.Items.Add(new PriceSnapshot
        {
            Id = new PriceSnapshotId(Guid.NewGuid()),
            InstrumentId = instrumentId,
            Date = new DateOnly(2026, 1, 31),
            ClosePriceAmount = 55m,
            PriceSourceLookupId = new PriceSourceLookupId(Guid.NewGuid()),
            CreatedAt = DateTime.UtcNow
        });
        prices.Items.Add(new PriceSnapshot
        {
            Id = new PriceSnapshotId(Guid.NewGuid()),
            InstrumentId = instrumentId,
            Date = new DateOnly(2026, 2, 28),
            ClosePriceAmount = 70m,
            PriceSourceLookupId = new PriceSourceLookupId(Guid.NewGuid()),
            CreatedAt = DateTime.UtcNow
        });

        var queryService = new PortfolioQueryService(
            instruments,
            new InMemorySectorLookupRepository(),
            trades,
            new InMemoryCashLedgerRepository(),
            prices,
            new InMemoryAlertEventRepository(),
            new HoldingsCalculator());

        var january = await queryService.GetMonthlyStateAsync(2026, 1, CancellationToken.None);
        var february = await queryService.GetMonthlyStateAsync(2026, 2, CancellationToken.None);

        Assert.Equal(275m, january.TotalMarketValue);
        Assert.Equal(350m, february.TotalMarketValue);
    }

}

internal sealed class InMemoryInstrumentRepository : IInstrumentRepository
{
    public List<Instrument> Items { get; } = [];

    public Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<Instrument>>(Items.ToList());

    public Task<Instrument?> GetByIdAsync(InstrumentId instrumentId, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == instrumentId));

    public Task AddAsync(Instrument instrument, CancellationToken cancellationToken)
    {
        Items.Add(instrument);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Instrument instrument, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(x => x.Id == instrument.Id);
        if (index >= 0)
        {
            Items[index] = instrument;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Instrument instrument, CancellationToken cancellationToken)
    {
        Items.RemoveAll(x => x.Id == instrument.Id);
        return Task.CompletedTask;
    }
}

