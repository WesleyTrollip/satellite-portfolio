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

public sealed class PortfolioQueryService(
    IInstrumentRepository instruments,
    ITradeRepository trades,
    ICashLedgerRepository cashEntries,
    IPriceSnapshotRepository prices,
    IAlertEventRepository alerts,
    IHoldingsCalculator holdingsCalculator)
{
    public async Task<PortfolioOverviewView> GetOverviewAsync(DateTime? asOf, CancellationToken cancellationToken)
    {
        var effectiveAsOf = asOf ?? DateTime.UtcNow;
        var allInstruments = await instruments.ListAsync(cancellationToken);
        var allTrades = await trades.ListAllAsync(cancellationToken);
        var allCash = await cashEntries.ListAllAsync(cancellationToken);
        var allPrices = await prices.ListAllAsync(cancellationToken);
        var currentAlerts = await alerts.ListCurrentAsync(cancellationToken);

        var snapshot = holdingsCalculator.CalculateSnapshot(allTrades, allCash, allPrices, effectiveAsOf);
        var holdings = MapHoldings(snapshot.Holdings, allInstruments);
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

    public async Task<IReadOnlyCollection<HoldingView>> GetHoldingsAsync(DateTime? asOf, CancellationToken cancellationToken)
    {
        var effectiveAsOf = asOf ?? DateTime.UtcNow;
        var allInstruments = await instruments.ListAsync(cancellationToken);
        var allTrades = await trades.ListAllAsync(cancellationToken);
        var allCash = await cashEntries.ListAllAsync(cancellationToken);
        var allPrices = await prices.ListAllAsync(cancellationToken);
        var snapshot = holdingsCalculator.CalculateSnapshot(allTrades, allCash, allPrices, effectiveAsOf);
        return MapHoldings(snapshot.Holdings, allInstruments);
    }

    public async Task<HoldingView?> GetHoldingAsync(InstrumentId instrumentId, DateTime? asOf, CancellationToken cancellationToken)
    {
        var holdings = await GetHoldingsAsync(asOf, cancellationToken);
        return holdings.SingleOrDefault(x => x.InstrumentId == instrumentId.Value);
    }

    private static List<HoldingView> MapHoldings(IEnumerable<Holding> holdings, IReadOnlyCollection<Instrument> instruments)
    {
        var instrumentMap = instruments.ToDictionary(x => x.Id, x => x);
        return holdings
            .OrderByDescending(x => x.MarketValue.Amount)
            .Select(h =>
            {
                instrumentMap.TryGetValue(h.InstrumentId, out var instrument);
                var missing = !h.HasPrice;
                return new HoldingView(
                    h.InstrumentId.Value,
                    instrument?.Symbol ?? "UNKNOWN",
                    instrument?.Sector,
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

