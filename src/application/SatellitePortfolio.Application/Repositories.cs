using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application;

public interface IInstrumentRepository
{
    Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken);
    Task<Instrument?> GetByIdAsync(InstrumentId instrumentId, CancellationToken cancellationToken);
    Task AddAsync(Instrument instrument, CancellationToken cancellationToken);
    Task UpdateAsync(Instrument instrument, CancellationToken cancellationToken);
    Task DeleteAsync(Instrument instrument, CancellationToken cancellationToken);
}

public interface ISectorLookupRepository
{
    Task<IReadOnlyCollection<SectorLookup>> ListAsync(string? search, bool? isActive, int skip, int take, CancellationToken cancellationToken);
    Task<SectorLookup?> GetByIdAsync(SectorLookupId sectorLookupId, CancellationToken cancellationToken);
    Task<SectorLookup?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(SectorLookup sectorLookup, CancellationToken cancellationToken);
    Task UpdateAsync(SectorLookup sectorLookup, CancellationToken cancellationToken);
    Task DeleteAsync(SectorLookup sectorLookup, CancellationToken cancellationToken);
}

public interface IPriceSourceLookupRepository
{
    Task<IReadOnlyCollection<PriceSourceLookup>> ListAsync(string? search, bool? isActive, int skip, int take, CancellationToken cancellationToken);
    Task<PriceSourceLookup?> GetByIdAsync(PriceSourceLookupId priceSourceLookupId, CancellationToken cancellationToken);
    Task<PriceSourceLookup?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(PriceSourceLookup priceSourceLookup, CancellationToken cancellationToken);
    Task UpdateAsync(PriceSourceLookup priceSourceLookup, CancellationToken cancellationToken);
    Task DeleteAsync(PriceSourceLookup priceSourceLookup, CancellationToken cancellationToken);
}

public interface ICorrectionReasonLookupRepository
{
    Task<IReadOnlyCollection<CorrectionReasonLookup>> ListAsync(string? search, bool? isActive, int skip, int take, CancellationToken cancellationToken);
    Task<CorrectionReasonLookup?> GetByIdAsync(CorrectionReasonLookupId correctionReasonLookupId, CancellationToken cancellationToken);
    Task<CorrectionReasonLookup?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(CorrectionReasonLookup correctionReasonLookup, CancellationToken cancellationToken);
    Task UpdateAsync(CorrectionReasonLookup correctionReasonLookup, CancellationToken cancellationToken);
    Task DeleteAsync(CorrectionReasonLookup correctionReasonLookup, CancellationToken cancellationToken);
}

public interface ITradeRepository
{
    Task<IReadOnlyCollection<Trade>> ListAsync(DateTime? from, DateTime? to, InstrumentId? instrumentId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Trade>> ListAllAsync(CancellationToken cancellationToken);
    Task<Trade?> GetByIdAsync(TradeId tradeId, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<Trade> trades, CancellationToken cancellationToken);
}

public interface ICashLedgerRepository
{
    Task<IReadOnlyCollection<CashLedgerEntry>> ListAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CashLedgerEntry>> ListAllAsync(CancellationToken cancellationToken);
    Task<CashLedgerEntry?> GetByIdAsync(CashEntryId cashEntryId, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<CashLedgerEntry> entries, CancellationToken cancellationToken);
}

public interface IPriceSnapshotRepository
{
    Task<IReadOnlyCollection<PriceSnapshot>> ListAsync(InstrumentId? instrumentId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PriceSnapshot>> ListAllAsync(CancellationToken cancellationToken);
    Task<PriceSnapshot?> GetByInstrumentAndDateAsync(InstrumentId instrumentId, DateOnly date, CancellationToken cancellationToken);
    Task AddAsync(PriceSnapshot snapshot, CancellationToken cancellationToken);
    Task UpdateAsync(PriceSnapshot snapshot, CancellationToken cancellationToken);
}

public interface IJournalRepository
{
    Task<IReadOnlyCollection<JournalEntry>> ListAsync(CancellationToken cancellationToken);
    Task<JournalEntry?> GetByIdAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken);
    Task AddAsync(JournalEntry journalEntry, CancellationToken cancellationToken);
    Task UpdateAsync(JournalEntry journalEntry, CancellationToken cancellationToken);
}

public interface IThesisRepository
{
    Task<IReadOnlyCollection<InvestmentThesis>> ListAsync(CancellationToken cancellationToken);
    Task<InvestmentThesis?> GetByIdAsync(ThesisId thesisId, CancellationToken cancellationToken);
    Task AddAsync(InvestmentThesis thesis, CancellationToken cancellationToken);
    Task UpdateAsync(InvestmentThesis thesis, CancellationToken cancellationToken);
}

public interface IJournalLinkRepository
{
    Task<IReadOnlyCollection<JournalEntryThesisLink>> ListThesisLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<JournalEntryInstrumentLink>> ListInstrumentLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken);
    Task AddThesisLinksAsync(IEnumerable<JournalEntryThesisLink> links, CancellationToken cancellationToken);
    Task AddInstrumentLinksAsync(IEnumerable<JournalEntryInstrumentLink> links, CancellationToken cancellationToken);
    Task RemoveThesisLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken);
    Task RemoveInstrumentLinksAsync(JournalEntryId journalEntryId, CancellationToken cancellationToken);
    Task<int> CountByInstrumentIdAsync(InstrumentId instrumentId, CancellationToken cancellationToken);
}

public interface IAlertEventRepository
{
    Task<IReadOnlyCollection<AlertEvent>> ListAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AlertEvent>> ListCurrentAsync(CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<AlertEvent> alertEvents, CancellationToken cancellationToken);
}

public interface IPortfolioRuleRepository
{
    Task<IReadOnlyCollection<PortfolioRule>> ListAsync(CancellationToken cancellationToken);
    Task<PortfolioRule?> GetByIdAsync(RuleId ruleId, CancellationToken cancellationToken);
    Task AddAsync(PortfolioRule rule, CancellationToken cancellationToken);
    Task UpdateAsync(PortfolioRule rule, CancellationToken cancellationToken);
}

public interface IPortfolioUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

