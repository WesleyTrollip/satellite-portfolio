using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application;

public interface IInstrumentRepository
{
    Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken);
    Task<Instrument?> GetByIdAsync(InstrumentId instrumentId, CancellationToken cancellationToken);
    Task AddAsync(Instrument instrument, CancellationToken cancellationToken);
    Task UpdateAsync(Instrument instrument, CancellationToken cancellationToken);
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
    Task<IReadOnlyCollection<PriceSnapshot>> ListAllAsync(CancellationToken cancellationToken);
}

public interface IAlertEventRepository
{
    Task<IReadOnlyCollection<AlertEvent>> ListCurrentAsync(CancellationToken cancellationToken);
}

public interface IPortfolioUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

