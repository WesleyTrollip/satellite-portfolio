namespace SatellitePortfolio.Domain;

public sealed class Portfolio
{
    public PortfolioId Id { get; init; }
    public string BaseCurrency { get; init; } = "EUR";
    public DateTime CreatedAt { get; init; }
}

public sealed class Instrument
{
    public InstrumentId Id { get; init; }
    public PortfolioId PortfolioId { get; init; }
    public string Symbol { get; init; } = null!;
    public string? Name { get; init; }
    public string? Sector { get; init; }
    public SectorLookupId? SectorLookupId { get; init; }
    public string Currency { get; init; } = "EUR";
    public DateTime CreatedAt { get; init; }
}

public enum TradeSide
{
    Buy = 1,
    Sell = 2,
    NonCashAcquisition = 3
}

public enum CostBasisMode
{
    Zero = 1,
    Custom = 2
}

public sealed class Trade
{
    public TradeId Id { get; init; }
    public PortfolioId PortfolioId { get; init; }
    public InstrumentId InstrumentId { get; init; }
    public TradeSide Side { get; init; }
    public decimal Quantity { get; init; }
    public decimal PriceAmount { get; init; }
    public decimal FeesAmount { get; init; }
    public CostBasisMode? CostBasisMode { get; init; }
    public decimal? CustomTotalCost { get; init; }
    public DateTime ExecutedAt { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }

    public CorrectionGroupId? CorrectionGroupId { get; init; }
    public TradeId? CorrectedByTradeId { get; init; }
    public CorrectionReasonLookupId? CorrectionReasonLookupId { get; init; }
    public bool IsCorrectionReversal { get; init; }
}

public enum CashEntryType
{
    Deposit = 1,
    Withdrawal = 2,
    Adjustment = 3
}

public sealed class CashLedgerEntry
{
    public CashEntryId Id { get; init; }
    public PortfolioId PortfolioId { get; init; }
    public CashEntryType Type { get; init; }
    public decimal Amount { get; init; }
    public DateTime OccurredAt { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }

    public CorrectionGroupId? CorrectionGroupId { get; init; }
    public CashEntryId? CorrectedByCashEntryId { get; init; }
    public bool IsCorrectionReversal { get; init; }
}

public sealed class PriceSnapshot
{
    public PriceSnapshotId Id { get; init; }
    public InstrumentId InstrumentId { get; init; }
    public DateOnly Date { get; init; }
    public decimal ClosePriceAmount { get; init; }
    public PriceSourceLookupId PriceSourceLookupId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class SectorLookup
{
    public SectorLookupId Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class PriceSourceLookup
{
    public PriceSourceLookupId Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class CorrectionReasonLookup
{
    public CorrectionReasonLookupId Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class JournalEntry
{
    public JournalEntryId Id { get; init; }
    public PortfolioId PortfolioId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime OccurredAt { get; init; }
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
    public string? Tags { get; init; }
}

public enum ThesisStatus
{
    Active = 1,
    Retired = 2
}

public sealed class InvestmentThesis
{
    public ThesisId Id { get; init; }
    public PortfolioId PortfolioId { get; init; }
    public InstrumentId? InstrumentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
    public ThesisStatus Status { get; init; }
}

public sealed class JournalEntryThesisLink
{
    public JournalEntryId JournalEntryId { get; init; }
    public ThesisId ThesisId { get; init; }
}

public sealed class JournalEntryInstrumentLink
{
    public JournalEntryId JournalEntryId { get; init; }
    public InstrumentId InstrumentId { get; init; }
}

public enum PortfolioRuleType
{
    MaxPositionSize = 1,
    MaxSectorConcentration = 2,
    MaxDrawdown = 3
}

public sealed class PortfolioRule
{
    public RuleId Id { get; init; }
    public PortfolioId PortfolioId { get; init; }
    public PortfolioRuleType Type { get; init; }
    public bool Enabled { get; init; }
    public string ParametersJson { get; init; } = "{}";
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public enum AlertSeverity
{
    Info = 1,
    Warn = 2,
    Critical = 3
}

public sealed class AlertEvent
{
    public AlertEventId Id { get; init; }
    public PortfolioId PortfolioId { get; init; }
    public RuleId RuleId { get; init; }
    public AlertSeverity Severity { get; init; }
    public DateTime TriggeredAt { get; init; }
    public DateTime AsOf { get; init; }
    public string Title { get; init; } = null!;
    public string DetailsJson { get; init; } = "{}";
}

