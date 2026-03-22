using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application;

public sealed record HoldingView(
    Guid InstrumentId,
    string Symbol,
    string? Sector,
    decimal Quantity,
    decimal AverageCost,
    decimal MarketValue,
    decimal UnrealizedPnl,
    decimal AllocationPercent,
    bool MissingPrice,
    string? MissingPriceExplanation);

public sealed record SectorAllocationView(
    string Sector,
    decimal AllocationPercent);

public sealed record AlertView(
    Guid AlertEventId,
    Guid RuleId,
    string Severity,
    string Title,
    DateTime TriggeredAt,
    DateTime AsOf,
    string DetailsJson);

public sealed record PortfolioOverviewView(
    DateTime AsOf,
    decimal CashBalance,
    decimal TotalMarketValue,
    decimal PortfolioValue,
    decimal TotalCost,
    decimal RealizedPnl,
    decimal UnrealizedPnl,
    IReadOnlyCollection<HoldingView> Holdings,
    IReadOnlyCollection<SectorAllocationView> SectorAllocations,
    IReadOnlyCollection<AlertView> CurrentAlerts);

public sealed record MonthlyPortfolioStateView(
    int Year,
    int Month,
    DateTime AsOf,
    decimal CashBalance,
    decimal TotalMarketValue,
    decimal PortfolioValue,
    decimal TotalCost,
    decimal RealizedPnl,
    decimal UnrealizedPnl,
    IReadOnlyCollection<HoldingView> Holdings,
    IReadOnlyCollection<SectorAllocationView> SectorAllocations);

public sealed class PortfolioQueryService(
    IInstrumentRepository instruments,
    ISectorLookupRepository sectors,
    ITradeRepository trades,
    ICashLedgerRepository cashEntries,
    IPriceSnapshotRepository prices,
    IAlertEventRepository alerts,
    IHoldingsCalculator holdingsCalculator)
{
    public async Task<PortfolioOverviewView> GetOverviewAsync(DateTime? asOf, CancellationToken cancellationToken)
    {
        var effectiveAsOf = asOf ?? EndOfUtcDay(DateTime.UtcNow);
        var allInstruments = await instruments.ListAsync(cancellationToken);
        var allSectors = await sectors.ListAsync(null, true, 0, 2000, cancellationToken);
        var allTrades = await trades.ListAllAsync(cancellationToken);
        var allCash = await cashEntries.ListAllAsync(cancellationToken);
        var allPrices = await prices.ListAllAsync(cancellationToken);
        var currentAlerts = await alerts.ListCurrentAsync(cancellationToken);

        var snapshot = holdingsCalculator.CalculateSnapshot(allTrades, allCash, allPrices, effectiveAsOf);
        var holdings = MapHoldings(snapshot.Holdings, allInstruments, allSectors);
        var sectorAllocations = BuildSectorAllocations(holdings);
        var alertViews = currentAlerts
            .OrderByDescending(x => x.TriggeredAt)
            .Select(a => new AlertView(
                a.Id.Value,
                a.RuleId.Value,
                a.Severity.ToString(),
                a.Title,
                a.TriggeredAt,
                a.AsOf,
                a.DetailsJson))
            .ToList();

        return new PortfolioOverviewView(
            effectiveAsOf,
            snapshot.Totals.CashBalance.Amount,
            snapshot.Totals.TotalMarketValue.Amount,
            snapshot.Totals.TotalMarketValue.Amount + snapshot.Totals.CashBalance.Amount,
            snapshot.Totals.TotalCost.Amount,
            snapshot.Totals.TotalRealizedPnl.Amount,
            snapshot.Totals.TotalUnrealizedPnl.Amount,
            holdings,
            sectorAllocations,
            alertViews);
    }

    private static DateTime EndOfUtcDay(DateTime utcNow)
    {
        var normalized = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : utcNow.ToUniversalTime();
        return new DateTime(
            normalized.Year,
            normalized.Month,
            normalized.Day,
            23,
            59,
            59,
            DateTimeKind.Utc);
    }

    public async Task<IReadOnlyCollection<HoldingView>> GetHoldingsAsync(DateTime? asOf, CancellationToken cancellationToken)
    {
        var effectiveAsOf = asOf ?? DateTime.UtcNow;
        var allInstruments = await instruments.ListAsync(cancellationToken);
        var allSectors = await sectors.ListAsync(null, true, 0, 2000, cancellationToken);
        var allTrades = await trades.ListAllAsync(cancellationToken);
        var allCash = await cashEntries.ListAllAsync(cancellationToken);
        var allPrices = await prices.ListAllAsync(cancellationToken);
        var snapshot = holdingsCalculator.CalculateSnapshot(allTrades, allCash, allPrices, effectiveAsOf);
        var holdings = MapHoldings(snapshot.Holdings, allInstruments, allSectors);

        return holdings;
    }

    public async Task<HoldingView?> GetHoldingAsync(InstrumentId instrumentId, DateTime? asOf, CancellationToken cancellationToken)
    {
        var holdings = await GetHoldingsAsync(asOf, cancellationToken);
        return holdings.SingleOrDefault(x => x.InstrumentId == instrumentId.Value);
    }

    public async Task<MonthlyPortfolioStateView> GetMonthlyStateAsync(int year, int month, CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            throw new InvalidOperationException("Month must be between 1 and 12.");
        }

        var asOf = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);
        var allInstruments = await instruments.ListAsync(cancellationToken);
        var allSectors = await sectors.ListAsync(null, true, 0, 2000, cancellationToken);
        var allTrades = await trades.ListAllAsync(cancellationToken);
        var allCash = await cashEntries.ListAllAsync(cancellationToken);
        var allPrices = await prices.ListAllAsync(cancellationToken);

        var snapshot = holdingsCalculator.CalculateSnapshot(allTrades, allCash, allPrices, asOf);
        var holdings = MapHoldings(snapshot.Holdings, allInstruments, allSectors);
        var sectorAllocations = BuildSectorAllocations(holdings);

        return new MonthlyPortfolioStateView(
            year,
            month,
            asOf,
            snapshot.Totals.CashBalance.Amount,
            snapshot.Totals.TotalMarketValue.Amount,
            snapshot.Totals.TotalMarketValue.Amount + snapshot.Totals.CashBalance.Amount,
            snapshot.Totals.TotalCost.Amount,
            snapshot.Totals.TotalRealizedPnl.Amount,
            snapshot.Totals.TotalUnrealizedPnl.Amount,
            holdings,
            sectorAllocations);
    }

    private static List<HoldingView> MapHoldings(
        IEnumerable<Holding> holdings,
        IReadOnlyCollection<Instrument> instruments,
        IReadOnlyCollection<SectorLookup> sectors)
    {
        var instrumentMap = instruments.ToDictionary(x => x.Id, x => x);
        var sectorMap = sectors.ToDictionary(x => x.Id, x => x.Name);
        return holdings
            .OrderByDescending(x => x.MarketValue.Amount)
            .Select(h =>
            {
                instrumentMap.TryGetValue(h.InstrumentId, out var instrument);
                var sector = instrument?.SectorLookupId is not null && sectorMap.TryGetValue(instrument.SectorLookupId.Value, out var sectorName)
                    ? sectorName
                    : instrument?.Sector;
                var missing = !h.HasPrice;
                return new HoldingView(
                    h.InstrumentId.Value,
                    instrument?.Symbol ?? "UNKNOWN",
                    sector,
                    h.Quantity,
                    h.AverageCost.Amount,
                    h.MarketValue.Amount,
                    h.UnrealizedPnl.Amount,
                    h.AllocationPercent,
                    missing,
                    missing ? "No EOD price available on or before requested as-of date." : null);
            })
            .ToList();
    }

    private static List<SectorAllocationView> BuildSectorAllocations(IEnumerable<HoldingView> holdings)
    {
        return holdings
            .Where(h => !h.MissingPrice && !string.IsNullOrWhiteSpace(h.Sector))
            .GroupBy(h => h.Sector!)
            .Select(g => new SectorAllocationView(g.Key, g.Sum(x => x.AllocationPercent)))
            .OrderByDescending(x => x.AllocationPercent)
            .ToList();
    }
}

