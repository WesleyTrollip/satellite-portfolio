namespace SatellitePortfolio.Domain;

public interface IHoldingsCalculator
{
    PortfolioSnapshot CalculateSnapshot(
        IEnumerable<Trade> trades,
        IEnumerable<CashLedgerEntry> cashEntries,
        IEnumerable<PriceSnapshot> prices,
        DateTime asOf);
}

public sealed class HoldingsCalculator : IHoldingsCalculator
{
    public PortfolioSnapshot CalculateSnapshot(
        IEnumerable<Trade> trades,
        IEnumerable<CashLedgerEntry> cashEntries,
        IEnumerable<PriceSnapshot> prices,
        DateTime asOf)
    {
        var eligibleTrades = trades
            .Where(t => t.ExecutedAt <= asOf)
            .OrderBy(t => t.ExecutedAt)
            .ThenBy(t => t.CreatedAt)
            .ToList();

        var eligibleCashEntries = cashEntries
            .Where(c => c.OccurredAt <= asOf)
            .ToList();

        var pricesByInstrument = prices
            .Where(p => p.Date <= DateOnly.FromDateTime(asOf))
            .GroupBy(p => p.InstrumentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Date).First());

        var perInstrument = new Dictionary<InstrumentId, PositionState>();
        var realizedPnl = 0m;
        var tradeCashFlow = 0m;

        foreach (var trade in eligibleTrades)
        {
            if (!perInstrument.TryGetValue(trade.InstrumentId, out var state))
            {
                state = new PositionState();
                perInstrument[trade.InstrumentId] = state;
            }

            if (trade.Side == TradeSide.Buy)
            {
                var buyTotalCost = trade.Quantity * trade.PriceAmount + trade.FeesAmount;
                state.Quantity += trade.Quantity;
                state.TotalCost += buyTotalCost;
                tradeCashFlow -= buyTotalCost;
                continue;
            }

            if (state.Quantity < trade.Quantity)
            {
                throw new InvalidOperationException(
                    $"Sell quantity exceeds holdings for instrument {trade.InstrumentId.Value}.");
            }

            var averageCostPerUnit = state.Quantity == 0m ? 0m : state.TotalCost / state.Quantity;
            var sellCostBasis = trade.Quantity * averageCostPerUnit;
            var proceedsNet = trade.Quantity * trade.PriceAmount - trade.FeesAmount;

            realizedPnl += proceedsNet - sellCostBasis;
            state.Quantity -= trade.Quantity;
            state.TotalCost -= sellCostBasis;
            tradeCashFlow += proceedsNet;

            if (state.Quantity == 0m)
            {
                state.TotalCost = 0m;
            }
        }

        var holdings = new List<Holding>();
        var totalMarketValue = 0m;
        var totalCost = 0m;
        var totalUnrealized = 0m;

        foreach (var (instrumentId, state) in perInstrument.Where(x => x.Value.Quantity > 0m))
        {
            var hasPrice = pricesByInstrument.TryGetValue(instrumentId, out var priceSnapshot);
            var marketValue = hasPrice ? state.Quantity * priceSnapshot!.ClosePriceAmount : 0m;
            var unrealized = hasPrice ? marketValue - state.TotalCost : 0m;

            totalMarketValue += marketValue;
            totalCost += state.TotalCost;
            totalUnrealized += unrealized;

            var avgCost = state.Quantity == 0m ? 0m : state.TotalCost / state.Quantity;

            holdings.Add(new Holding(
                instrumentId,
                state.Quantity,
                new Money(avgCost),
                new Money(marketValue),
                new Money(unrealized),
                0m,
                hasPrice));
        }

        var cashBalance = eligibleCashEntries.Sum(c => c.Amount) + tradeCashFlow;
        var allocationDenominator = totalMarketValue + cashBalance;

        var holdingsWithAllocation = holdings
            .Select(h => h with
            {
                AllocationPercent = allocationDenominator == 0m || !h.HasPrice
                    ? 0m
                    : h.MarketValue.Amount / allocationDenominator
            })
            .ToList();

        var totals = new PortfolioTotals(
            new Money(totalMarketValue),
            new Money(totalCost),
            new Money(totalUnrealized),
            new Money(realizedPnl),
            new Money(cashBalance));

        return new PortfolioSnapshot(holdingsWithAllocation, totals);
    }

    private sealed class PositionState
    {
        public decimal Quantity { get; set; }
        public decimal TotalCost { get; set; }
    }
}

