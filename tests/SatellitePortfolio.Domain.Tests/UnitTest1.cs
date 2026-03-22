using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Domain.Tests;

public class HoldingsCalculatorTests
{
    [Fact]
    public void Calculates_AverageCost_And_RealizedPnl_For_PartialSell()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var calculator = new HoldingsCalculator();

        var trades = new[]
        {
            new Trade
            {
                Id = new TradeId(Guid.NewGuid()),
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Side = TradeSide.Buy,
                Quantity = 10m,
                PriceAmount = 100m,
                FeesAmount = 10m,
                ExecutedAt = new DateTime(2026, 1, 1),
                CreatedAt = new DateTime(2026, 1, 1)
            },
            new Trade
            {
                Id = new TradeId(Guid.NewGuid()),
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Side = TradeSide.Sell,
                Quantity = 4m,
                PriceAmount = 120m,
                FeesAmount = 5m,
                ExecutedAt = new DateTime(2026, 1, 5),
                CreatedAt = new DateTime(2026, 1, 5)
            }
        };

        var prices = new[]
        {
            new PriceSnapshot
            {
                Id = new PriceSnapshotId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Date = new DateOnly(2026, 1, 5),
                ClosePriceAmount = 130m,
                PriceSourceLookupId = new PriceSourceLookupId(Guid.NewGuid()),
                CreatedAt = DateTime.UtcNow
            }
        };

        var snapshot = calculator.CalculateSnapshot(trades, Array.Empty<CashLedgerEntry>(), prices, new DateTime(2026, 1, 5));
        var holding = Assert.Single(snapshot.Holdings);

        Assert.Equal(6m, holding.Quantity);
        Assert.Equal(101m, holding.AverageCost.Amount);
        Assert.Equal(780m, holding.MarketValue.Amount);
        Assert.Equal(71m, snapshot.Totals.TotalRealizedPnl.Amount);
        Assert.Equal(174m, holding.UnrealizedPnl.Amount);
    }

    [Fact]
    public void Handles_MissingPrice_By_KeepingHolding_And_MarkingUnavailable()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var calculator = new HoldingsCalculator();

        var trades = new[]
        {
            new Trade
            {
                Id = new TradeId(Guid.NewGuid()),
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Side = TradeSide.Buy,
                Quantity = 2m,
                PriceAmount = 50m,
                FeesAmount = 0m,
                ExecutedAt = new DateTime(2026, 2, 1),
                CreatedAt = new DateTime(2026, 2, 1)
            }
        };

        var snapshot = calculator.CalculateSnapshot(trades, Array.Empty<CashLedgerEntry>(), Array.Empty<PriceSnapshot>(), new DateTime(2026, 2, 2));
        var holding = Assert.Single(snapshot.Holdings);

        Assert.False(holding.HasPrice);
        Assert.Equal(2m, holding.Quantity);
        Assert.Equal(0m, holding.MarketValue.Amount);
        Assert.Equal(0m, holding.UnrealizedPnl.Amount);
    }

    [Fact]
    public void Includes_CashLedger_And_TradeCashFlows_In_CashBalance()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var calculator = new HoldingsCalculator();

        var trades = new[]
        {
            new Trade
            {
                Id = new TradeId(Guid.NewGuid()),
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Side = TradeSide.Buy,
                Quantity = 1m,
                PriceAmount = 100m,
                FeesAmount = 0m,
                ExecutedAt = new DateTime(2026, 1, 2),
                CreatedAt = new DateTime(2026, 1, 2)
            }
        };

        var cashEntries = new[]
        {
            new CashLedgerEntry
            {
                Id = new CashEntryId(Guid.NewGuid()),
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                Type = CashEntryType.Deposit,
                Amount = 1_000m,
                OccurredAt = new DateTime(2026, 1, 1),
                CreatedAt = new DateTime(2026, 1, 1)
            }
        };

        var snapshot = calculator.CalculateSnapshot(trades, cashEntries, Array.Empty<PriceSnapshot>(), new DateTime(2026, 1, 3));
        Assert.Equal(900m, snapshot.Totals.CashBalance.Amount);
    }

    [Fact]
    public void Uses_LastAvailablePrice_OnOrBefore_AsOfDate()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var calculator = new HoldingsCalculator();

        var trades = new[]
        {
            new Trade
            {
                Id = new TradeId(Guid.NewGuid()),
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Side = TradeSide.Buy,
                Quantity = 1m,
                PriceAmount = 100m,
                FeesAmount = 0m,
                ExecutedAt = new DateTime(2026, 2, 1),
                CreatedAt = new DateTime(2026, 2, 1)
            }
        };

        var prices = new[]
        {
            new PriceSnapshot
            {
                Id = new PriceSnapshotId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Date = new DateOnly(2026, 2, 15),
                ClosePriceAmount = 90m,
                PriceSourceLookupId = new PriceSourceLookupId(Guid.NewGuid()),
                CreatedAt = DateTime.UtcNow
            },
            new PriceSnapshot
            {
                Id = new PriceSnapshotId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Date = new DateOnly(2026, 2, 28),
                ClosePriceAmount = 110m,
                PriceSourceLookupId = new PriceSourceLookupId(Guid.NewGuid()),
                CreatedAt = DateTime.UtcNow
            }
        };

        var snapshot = calculator.CalculateSnapshot(trades, Array.Empty<CashLedgerEntry>(), prices, new DateTime(2026, 2, 20));
        var holding = Assert.Single(snapshot.Holdings);
        Assert.Equal(90m, holding.MarketValue.Amount);
    }

    [Fact]
    public void NonCashAcquisition_WithZeroBasis_IncreasesQuantityWithoutReducingCash()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var calculator = new HoldingsCalculator();
        var trades = new[]
        {
            new Trade
            {
                Id = new TradeId(Guid.NewGuid()),
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Side = TradeSide.NonCashAcquisition,
                Quantity = 10m,
                PriceAmount = 0m,
                FeesAmount = 0m,
                CostBasisMode = CostBasisMode.Zero,
                CustomTotalCost = null,
                ExecutedAt = new DateTime(2026, 3, 1),
                CreatedAt = new DateTime(2026, 3, 1)
            }
        };

        var snapshot = calculator.CalculateSnapshot(trades, Array.Empty<CashLedgerEntry>(), Array.Empty<PriceSnapshot>(), new DateTime(2026, 3, 2));
        var holding = Assert.Single(snapshot.Holdings);
        Assert.Equal(10m, holding.Quantity);
        Assert.Equal(0m, holding.AverageCost.Amount);
        Assert.Equal(0m, snapshot.Totals.CashBalance.Amount);
    }

    [Fact]
    public void NonCashAcquisition_WithCustomBasis_DrivesRealizedPnlOnSell()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var calculator = new HoldingsCalculator();
        var trades = new[]
        {
            new Trade
            {
                Id = new TradeId(Guid.NewGuid()),
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Side = TradeSide.NonCashAcquisition,
                Quantity = 10m,
                PriceAmount = 0m,
                FeesAmount = 0m,
                CostBasisMode = CostBasisMode.Custom,
                CustomTotalCost = 800m,
                ExecutedAt = new DateTime(2026, 3, 1),
                CreatedAt = new DateTime(2026, 3, 1)
            },
            new Trade
            {
                Id = new TradeId(Guid.NewGuid()),
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                InstrumentId = instrumentId,
                Side = TradeSide.Sell,
                Quantity = 4m,
                PriceAmount = 100m,
                FeesAmount = 0m,
                ExecutedAt = new DateTime(2026, 3, 2),
                CreatedAt = new DateTime(2026, 3, 2)
            }
        };

        var snapshot = calculator.CalculateSnapshot(trades, Array.Empty<CashLedgerEntry>(), Array.Empty<PriceSnapshot>(), new DateTime(2026, 3, 2));
        var holding = Assert.Single(snapshot.Holdings);
        Assert.Equal(6m, holding.Quantity);
        Assert.Equal(80m, holding.AverageCost.Amount);
        Assert.Equal(80m, snapshot.Totals.TotalRealizedPnl.Amount);
    }
}
