using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application.Tests;

public class PriceSnapshotServiceTests
{
    [Fact]
    public async Task Upsert_IsIdempotent_PerInstrumentAndDate()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var instruments = new InMemoryInstrumentRepository();
        instruments.Items.Add(new Instrument
        {
            Id = instrumentId,
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Symbol = "MSFT",
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow
        });

        var repository = new InMemoryPriceSnapshotRepository();
        var priceSources = new InMemoryPriceSourceLookupRepository();
        var defaultSource = new PriceSourceLookup
        {
            Id = new PriceSourceLookupId(Guid.NewGuid()),
            Code = "MANUAL",
            Name = "Manual",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        priceSources.Items.Add(defaultSource);
        var service = new PriceSnapshotService(repository, instruments, priceSources, new InMemoryUnitOfWork());

        var first = await service.UpsertAsync(
            new UpsertPriceSnapshotRequest(instrumentId.Value, new DateOnly(2026, 3, 18), 250m, defaultSource.Id.Value),
            CancellationToken.None);

        var second = await service.UpsertAsync(
            new UpsertPriceSnapshotRequest(instrumentId.Value, new DateOnly(2026, 3, 18), 255m, defaultSource.Id.Value),
            CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(255m, repository.Items[0].ClosePriceAmount);
    }
}

