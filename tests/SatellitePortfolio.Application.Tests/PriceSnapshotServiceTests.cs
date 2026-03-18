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
        var service = new PriceSnapshotService(repository, instruments, new InMemoryUnitOfWork());

        var first = await service.UpsertAsync(
            new UpsertPriceSnapshotRequest(instrumentId.Value, new DateOnly(2026, 3, 18), 250m, PriceSnapshotSource.Manual),
            CancellationToken.None);

        var second = await service.UpsertAsync(
            new UpsertPriceSnapshotRequest(instrumentId.Value, new DateOnly(2026, 3, 18), 255m, PriceSnapshotSource.Manual),
            CancellationToken.None);

        Assert.Single(repository.Items);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(255m, repository.Items[0].ClosePriceAmount);
    }
}

