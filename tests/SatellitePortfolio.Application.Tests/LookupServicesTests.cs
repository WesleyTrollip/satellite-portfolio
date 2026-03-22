using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application.Tests;

public class LookupServicesTests
{
    [Fact]
    public async Task SectorLookupCrud_SupportsCreateUpdateDeactivate()
    {
        var repository = new InMemorySectorLookupRepository();
        var service = new SectorLookupService(repository, new InMemoryInstrumentRepository(), new InMemoryUnitOfWork());

        var created = await service.CreateAsync(new UpsertLookupRequest("tech", "Technology"), CancellationToken.None);
        Assert.Equal("TECH", created.Code);

        var updated = await service.UpdateAsync(new SectorLookupId(created.Id), new UpsertLookupRequest("TECH", "Tech Sector", true), CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal("Tech Sector", updated!.Name);

        var deactivated = await service.DeactivateAsync(new SectorLookupId(created.Id), CancellationToken.None);
        Assert.NotNull(deactivated);
        Assert.False(deactivated!.IsActive);
    }

    [Fact]
    public async Task InstrumentCreate_RejectsInactiveSectorLookup()
    {
        var sectorRepository = new InMemorySectorLookupRepository();
        var sector = new SectorLookup
        {
            Id = new SectorLookupId(Guid.NewGuid()),
            Code = "ENERGY",
            Name = "Energy",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        sectorRepository.Items.Add(sector);

        var service = new InstrumentService(
            new InMemoryInstrumentRepository(),
            sectorRepository,
            new InMemoryTradeRepository(),
            new InMemoryPriceSnapshotRepository(),
            new InMemoryThesisRepository(),
            new InMemoryJournalLinkRepository(),
            new InMemoryUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                new CreateInstrumentRequest("XOM", "Exxon Mobil", sector.Id.Value, "USD"),
                CancellationToken.None));
    }

    [Fact]
    public async Task PriceSnapshotUpsert_RejectsInvalidPriceSourceLookup()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var instruments = new InMemoryInstrumentRepository();
        instruments.Items.Add(new Instrument
        {
            Id = instrumentId,
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Symbol = "MSFT",
            Currency = "USD",
            CreatedAt = DateTime.UtcNow
        });

        var service = new PriceSnapshotService(
            new InMemoryPriceSnapshotRepository(),
            instruments,
            new InMemoryPriceSourceLookupRepository(),
            new InMemoryUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertAsync(
                new UpsertPriceSnapshotRequest(instrumentId.Value, new DateOnly(2026, 3, 21), 320.5m, Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task SectorLookupDelete_IsBlocked_WhenReferencedByInstrument()
    {
        var sectorRepository = new InMemorySectorLookupRepository();
        var instrumentRepository = new InMemoryInstrumentRepository();
        var sectorId = new SectorLookupId(Guid.NewGuid());
        sectorRepository.Items.Add(new SectorLookup
        {
            Id = sectorId,
            Code = "TECH",
            Name = "Technology",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        instrumentRepository.Items.Add(new Instrument
        {
            Id = new InstrumentId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Symbol = "MSFT",
            SectorLookupId = sectorId,
            Sector = "Technology",
            Currency = "USD",
            CreatedAt = DateTime.UtcNow
        });

        var service = new SectorLookupService(sectorRepository, instrumentRepository, new InMemoryUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(sectorId, CancellationToken.None));
    }

    [Fact]
    public async Task InstrumentDelete_IsBlocked_WhenReferencedByTrades()
    {
        var instrumentRepository = new InMemoryInstrumentRepository();
        var sectorRepository = new InMemorySectorLookupRepository();
        var tradeRepository = new InMemoryTradeRepository();
        var instrumentId = new InstrumentId(Guid.NewGuid());
        instrumentRepository.Items.Add(new Instrument
        {
            Id = instrumentId,
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Symbol = "AAPL",
            Currency = "USD",
            CreatedAt = DateTime.UtcNow
        });

        tradeRepository.Items.Add(new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            InstrumentId = instrumentId,
            Side = TradeSide.Buy,
            Quantity = 1m,
            PriceAmount = 100m,
            FeesAmount = 0m,
            ExecutedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        var service = new InstrumentService(
            instrumentRepository,
            sectorRepository,
            tradeRepository,
            new InMemoryPriceSnapshotRepository(),
            new InMemoryThesisRepository(),
            new InMemoryJournalLinkRepository(),
            new InMemoryUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(instrumentId, CancellationToken.None));
    }
}
