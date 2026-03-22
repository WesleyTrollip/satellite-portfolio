namespace SatellitePortfolio.Domain;

public static class CorrectionFactory
{
    public static (Trade reversal, Trade replacement) CreateTradeCorrection(
        Trade original,
        Trade replacementDraft,
        DateTime createdAt)
    {
        var correctionGroupId = new CorrectionGroupId(Guid.NewGuid());

        var reversal = new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = original.PortfolioId,
            InstrumentId = original.InstrumentId,
            Side = original.Side == TradeSide.Buy ? TradeSide.Sell : TradeSide.Buy,
            Quantity = original.Quantity,
            PriceAmount = original.PriceAmount,
            FeesAmount = original.FeesAmount,
            ExecutedAt = replacementDraft.ExecutedAt,
            Notes = $"Reversal for trade {original.Id.Value}",
            CreatedAt = createdAt,
            CorrectionGroupId = correctionGroupId,
            CorrectedByTradeId = replacementDraft.Id,
            IsCorrectionReversal = true
        };

        var replacement = new Trade
        {
            Id = replacementDraft.Id,
            PortfolioId = replacementDraft.PortfolioId,
            InstrumentId = replacementDraft.InstrumentId,
            Side = replacementDraft.Side,
            Quantity = replacementDraft.Quantity,
            PriceAmount = replacementDraft.PriceAmount,
            FeesAmount = replacementDraft.FeesAmount,
            ExecutedAt = replacementDraft.ExecutedAt,
            Notes = replacementDraft.Notes,
            CreatedAt = createdAt,
            CorrectionGroupId = correctionGroupId,
            CorrectedByTradeId = replacementDraft.CorrectedByTradeId,
            IsCorrectionReversal = false
        };

        return (reversal, replacement);
    }

    public static (CashLedgerEntry reversal, CashLedgerEntry replacement) CreateCashCorrection(
        CashLedgerEntry original,
        CashLedgerEntry replacementDraft,
        DateTime createdAt)
    {
        var correctionGroupId = new CorrectionGroupId(Guid.NewGuid());

        var reversal = new CashLedgerEntry
        {
            Id = new CashEntryId(Guid.NewGuid()),
            PortfolioId = original.PortfolioId,
            Type = CashEntryType.Adjustment,
            Amount = -original.Amount,
            OccurredAt = replacementDraft.OccurredAt,
            Notes = $"Reversal for cash entry {original.Id.Value}",
            CreatedAt = createdAt,
            CorrectionGroupId = correctionGroupId,
            CorrectedByCashEntryId = replacementDraft.Id,
            IsCorrectionReversal = true
        };

        var replacement = new CashLedgerEntry
        {
            Id = replacementDraft.Id,
            PortfolioId = replacementDraft.PortfolioId,
            Type = replacementDraft.Type,
            Amount = replacementDraft.Amount,
            OccurredAt = replacementDraft.OccurredAt,
            Notes = replacementDraft.Notes,
            CreatedAt = createdAt,
            CorrectionGroupId = correctionGroupId,
            CorrectedByCashEntryId = replacementDraft.CorrectedByCashEntryId,
            IsCorrectionReversal = false
        };

        return (reversal, replacement);
    }
}

