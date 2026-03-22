using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application;

public sealed record UpsertPriceSnapshotRequest(
    Guid InstrumentId,
    DateOnly Date,
    decimal ClosePriceAmount,
    Guid PriceSourceLookupId);

public sealed class PriceSnapshotService(
    IPriceSnapshotRepository priceSnapshots,
    IInstrumentRepository instruments,
    IPriceSourceLookupRepository priceSources,
    IPortfolioUnitOfWork unitOfWork)
{
    public Task<IReadOnlyCollection<PriceSnapshot>> ListAsync(
        InstrumentId? instrumentId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
        => priceSnapshots.ListAsync(instrumentId, from, to, cancellationToken);

    public async Task<PriceSnapshot> UpsertAsync(UpsertPriceSnapshotRequest request, CancellationToken cancellationToken)
    {
        if (request.ClosePriceAmount < 0m)
        {
            throw new InvalidOperationException("Close price cannot be negative.");
        }

        var instrumentId = new InstrumentId(request.InstrumentId);
        var instrument = await instruments.GetByIdAsync(instrumentId, cancellationToken);
        if (instrument is null)
        {
            throw new InvalidOperationException($"Instrument '{request.InstrumentId}' was not found.");
        }

        var priceSourceLookupId = new PriceSourceLookupId(request.PriceSourceLookupId);
        var priceSource = await priceSources.GetByIdAsync(priceSourceLookupId, cancellationToken);
        if (priceSource is null || !priceSource.IsActive)
        {
            throw new InvalidOperationException($"Price source '{request.PriceSourceLookupId}' is invalid or inactive.");
        }

        var existing = await priceSnapshots.GetByInstrumentAndDateAsync(instrumentId, request.Date, cancellationToken);
        if (existing is null)
        {
            var created = new PriceSnapshot
            {
                Id = new PriceSnapshotId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Date = request.Date,
                ClosePriceAmount = request.ClosePriceAmount,
                PriceSourceLookupId = priceSourceLookupId,
                CreatedAt = DateTime.UtcNow
            };

            await priceSnapshots.AddAsync(created, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return created;
        }

        var updated = new PriceSnapshot
        {
            Id = existing.Id,
            InstrumentId = existing.InstrumentId,
            Date = existing.Date,
            ClosePriceAmount = request.ClosePriceAmount,
            PriceSourceLookupId = priceSourceLookupId,
            CreatedAt = existing.CreatedAt
        };

        await priceSnapshots.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return updated;
    }
}

