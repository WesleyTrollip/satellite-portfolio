using Microsoft.EntityFrameworkCore;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Infrastructure;

public sealed class InstrumentRepository(SatellitePortfolioDbContext dbContext) : IInstrumentRepository
{
    public async Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken)
        => await dbContext.Instruments.OrderBy(x => x.Symbol).ToListAsync(cancellationToken);

    public Task<Instrument?> GetByIdAsync(InstrumentId instrumentId, CancellationToken cancellationToken)
        => dbContext.Instruments.SingleOrDefaultAsync(x => x.Id == instrumentId, cancellationToken);

    public async Task AddAsync(Instrument instrument, CancellationToken cancellationToken)
        => await dbContext.Instruments.AddAsync(instrument, cancellationToken);

    public Task UpdateAsync(Instrument instrument, CancellationToken cancellationToken)
    {
        dbContext.Instruments.Update(instrument);
        return Task.CompletedTask;
    }
}

public sealed class TradeRepository(SatellitePortfolioDbContext dbContext) : ITradeRepository
{
    public async Task<IReadOnlyCollection<Trade>> ListAsync(
        DateTime? from,
        DateTime? to,
        InstrumentId? instrumentId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Trades.AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(x => x.ExecutedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.ExecutedAt <= to.Value);
        }

        if (instrumentId.HasValue)
        {
            query = query.Where(x => x.InstrumentId == instrumentId.Value);
        }

        return await query
            .OrderByDescending(x => x.ExecutedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Trade>> ListAllAsync(CancellationToken cancellationToken)
        => await dbContext.Trades.ToListAsync(cancellationToken);

    public Task<Trade?> GetByIdAsync(TradeId tradeId, CancellationToken cancellationToken)
        => dbContext.Trades.SingleOrDefaultAsync(x => x.Id == tradeId, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Trade> trades, CancellationToken cancellationToken)
        => await dbContext.Trades.AddRangeAsync(trades, cancellationToken);
}

public sealed class CashLedgerRepository(SatellitePortfolioDbContext dbContext) : ICashLedgerRepository
{
    public async Task<IReadOnlyCollection<CashLedgerEntry>> ListAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CashLedgerEntries.AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(x => x.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.OccurredAt <= to.Value);
        }

        return await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CashLedgerEntry>> ListAllAsync(CancellationToken cancellationToken)
        => await dbContext.CashLedgerEntries.ToListAsync(cancellationToken);

    public Task<CashLedgerEntry?> GetByIdAsync(CashEntryId cashEntryId, CancellationToken cancellationToken)
        => dbContext.CashLedgerEntries.SingleOrDefaultAsync(x => x.Id == cashEntryId, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<CashLedgerEntry> entries, CancellationToken cancellationToken)
        => await dbContext.CashLedgerEntries.AddRangeAsync(entries, cancellationToken);
}

public sealed class PriceSnapshotRepository(SatellitePortfolioDbContext dbContext) : IPriceSnapshotRepository
{
    public async Task<IReadOnlyCollection<PriceSnapshot>> ListAllAsync(CancellationToken cancellationToken)
        => await dbContext.PriceSnapshots.ToListAsync(cancellationToken);
}

public sealed class AlertEventRepository(SatellitePortfolioDbContext dbContext) : IAlertEventRepository
{
    public async Task<IReadOnlyCollection<AlertEvent>> ListCurrentAsync(CancellationToken cancellationToken)
    {
        var latestPerRule = await dbContext.AlertEvents
            .GroupBy(x => x.RuleId)
            .Select(g => g
                .OrderByDescending(x => x.TriggeredAt)
                .First())
            .ToListAsync(cancellationToken);

        return latestPerRule;
    }
}

public sealed class PortfolioUnitOfWork(SatellitePortfolioDbContext dbContext) : IPortfolioUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await dbContext.SaveChangesAsync(cancellationToken);
}

