namespace SatellitePortfolio.Domain;

public readonly record struct PortfolioId(Guid Value);

public readonly record struct InstrumentId(Guid Value);

public readonly record struct TradeId(Guid Value);

public readonly record struct CashEntryId(Guid Value);

public readonly record struct PriceSnapshotId(Guid Value);

public readonly record struct SectorLookupId(Guid Value);

public readonly record struct PriceSourceLookupId(Guid Value);

public readonly record struct CorrectionReasonLookupId(Guid Value);

public readonly record struct JournalEntryId(Guid Value);

public readonly record struct ThesisId(Guid Value);

public readonly record struct RuleId(Guid Value);

public readonly record struct AlertEventId(Guid Value);

public readonly record struct CorrectionGroupId(Guid Value);

public readonly record struct Money
{
    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency = "EUR")
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(string currency = "EUR") => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException("Cannot add money in different currencies.");
        }

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException("Cannot subtract money in different currencies.");
        }

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}

