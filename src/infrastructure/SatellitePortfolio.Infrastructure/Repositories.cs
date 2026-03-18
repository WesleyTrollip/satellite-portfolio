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
    public async Task<IReadOnlyCollection<PriceSnapshot>> ListAsync(
        InstrumentId? instrumentId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PriceSnapshots.AsQueryable();
        if (instrumentId.HasValue)
        {
            query = query.Where(x => x.InstrumentId == instrumentId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.Date >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.Date <= to.Value);
        }

        return await query
            .OrderBy(x => x.InstrumentId)
            .ThenByDescending(x => x.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PriceSnapshot>> ListAllAsync(CancellationToken cancellationToken)
        => await dbContext.PriceSnapshots.ToListAsync(cancellationToken);

    public Task<PriceSnapshot?> GetByInstrumentAndDateAsync(InstrumentId instrumentId, DateOnly date, CancellationToken cancellationToken)
        => dbContext.PriceSnapshots.SingleOrDefaultAsync(x => x.InstrumentId == instrumentId && x.Date == date, cancellationToken);

    public async Task AddAsync(PriceSnapshot snapshot, CancellationToken cancellationToken)
        => await dbContext.PriceSnapshots.AddAsync(snapshot, cancellationToken);

    public Task UpdateAsync(PriceSnapshot snapshot, CancellationToken cancellationToken)
    {
        dbContext.PriceSnapshots.Update(snapshot);
        return Task.CompletedTask;
    }
}

public sealed class JournalRepository(SatellitePortfolioDbContext dbContext) : IJournalRepository
{
    public async Task<IReadOnlyCollection<JournalEntry>> ListAsync(CancellationToken cancellationToken)
        => await dbContext.JournalEntries.OrderByDescending(x => x.OccurredAt).ToListAsync(cancellationToken);

    public Task<JournalEntry?> GetByIdAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
        => dbContext.JournalEntries.SingleOrDefaultAsync(x => x.Id == journalEntryId, cancellationToken);

    public async Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken)
        => await dbContext.JournalEntries.AddAsync(journalEntry, cancellationToken);

    public Task UpdateAsync(JournalEntry journalEntry, CancellationToken cancellationToken)
    {
        dbContext.JournalEntries.Update(journalEntry);
        return Task.CompletedTask;
    }
}

public sealed class ThesisRepository(SatellitePortfolioDbContext dbContext) : IThesisRepository
{
    public async Task<IReadOnlyCollection<InvestmentThesis>> ListAsync(CancellationToken cancellationToken)
        => await dbContext.InvestmentTheses.OrderByDescending(x => x.UpdatedAt).ToListAsync(cancellationToken);

    public Task<InvestmentThesis?> GetByIdAsync(ThesisId thesisId, CancellationToken cancellationToken)
        => dbContext.InvestmentTheses.SingleOrDefaultAsync(x => x.Id == thesisId, cancellationToken);

    public async Task AddAsync(InvestmentThesis thesis, CancellationToken cancellationToken)
        => await dbContext.InvestmentTheses.AddAsync(thesis, cancellationToken);

    public Task UpdateAsync(InvestmentThesis thesis, CancellationToken cancellationToken)
    {
        dbContext.InvestmentTheses.Update(thesis);
        return Task.CompletedTask;
    }
}

public sealed class JournalLinkRepository(SatellitePortfolioDbContext dbContext) : IJournalLinkRepository
{
    public async Task<IReadOnlyCollection<JournalEntryThesisLink>> ListThesisLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
        => await dbContext.JournalEntryThesisLinks.Where(x => x.JournalEntryId == journalEntryId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<JournalEntryInstrumentLink>> ListInstrumentLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
        => await dbContext.JournalEntryInstrumentLinks.Where(x => x.JournalEntryId == journalEntryId).ToListAsync(cancellationToken);

    public async Task AddThesisLinksAsync(IEnumerable<JournalEntryThesisLink> links, CancellationToken cancellationToken)
        => await dbContext.JournalEntryThesisLinks.AddRangeAsync(links, cancellationToken);

    public async Task AddInstrumentLinksAsync(IEnumerable<JournalEntryInstrumentLink> links, CancellationToken cancellationToken)
        => await dbContext.JournalEntryInstrumentLinks.AddRangeAsync(links, cancellationToken);

    public async Task RemoveThesisLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
    {
        var links = await dbContext.JournalEntryThesisLinks.Where(x => x.JournalEntryId == journalEntryId).ToListAsync(cancellationToken);
        if (links.Count > 0)
        {
            dbContext.JournalEntryThesisLinks.RemoveRange(links);
        }
    }

    public async Task RemoveInstrumentLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken)
    {
        var links = await dbContext.JournalEntryInstrumentLinks.Where(x => x.JournalEntryId == journalEntryId).ToListAsync(cancellationToken);
        if (links.Count > 0)
        {
            dbContext.JournalEntryInstrumentLinks.RemoveRange(links);
        }
    }
}

public sealed class PortfolioRuleRepository(SatellitePortfolioDbContext dbContext) : IPortfolioRuleRepository
{
    public async Task<IReadOnlyCollection<PortfolioRule>> ListAsync(CancellationToken cancellationToken)
        => await dbContext.PortfolioRules.OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);

    public Task<PortfolioRule?> GetByIdAsync(RuleId ruleId, CancellationToken cancellationToken)
        => dbContext.PortfolioRules.SingleOrDefaultAsync(x => x.Id == ruleId, cancellationToken);

    public async Task AddAsync(PortfolioRule rule, CancellationToken cancellationToken)
        => await dbContext.PortfolioRules.AddAsync(rule, cancellationToken);

    public Task UpdateAsync(PortfolioRule rule, CancellationToken cancellationToken)
    {
        dbContext.PortfolioRules.Update(rule);
        return Task.CompletedTask;
    }
}

public sealed class AlertEventRepository(SatellitePortfolioDbContext dbContext) : IAlertEventRepository
{
    public async Task<IReadOnlyCollection<AlertEvent>> ListAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var query = dbContext.AlertEvents.AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(x => x.TriggeredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.TriggeredAt <= to.Value);
        }

        return await query.OrderByDescending(x => x.TriggeredAt).ToListAsync(cancellationToken);
    }

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

    public async Task AddRangeAsync(IEnumerable<AlertEvent> alertEvents, CancellationToken cancellationToken)
        => await dbContext.AlertEvents.AddRangeAsync(alertEvents, cancellationToken);
}

public sealed class PortfolioUnitOfWork(SatellitePortfolioDbContext dbContext) : IPortfolioUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await dbContext.SaveChangesAsync(cancellationToken);
}

