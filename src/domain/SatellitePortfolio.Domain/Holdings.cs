namespace SatellitePortfolio.Domain;

public sealed record Holding(
    InstrumentId InstrumentId,
    decimal Quantity,
    Money AverageCost,
    Money MarketValue,
    Money UnrealizedPnl,
    decimal AllocationPercent,
    bool HasPrice);

public sealed record PortfolioTotals(
    Money TotalMarketValue,
    Money TotalCost,
    Money TotalUnrealizedPnl,
    Money TotalRealizedPnl,
    Money CashBalance);

public sealed record PortfolioSnapshot(
    IReadOnlyCollection<Holding> Holdings,
    PortfolioTotals Totals);

