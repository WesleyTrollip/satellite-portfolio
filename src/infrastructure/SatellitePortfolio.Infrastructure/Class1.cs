using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Infrastructure;

public class SatellitePortfolioDbContext(DbContextOptions<SatellitePortfolioDbContext> options)
    : DbContext(options)
{
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<CashLedgerEntry> CashLedgerEntries => Set<CashLedgerEntry>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<InvestmentThesis> InvestmentTheses => Set<InvestmentThesis>();
    public DbSet<JournalEntryThesisLink> JournalEntryThesisLinks => Set<JournalEntryThesisLink>();
    public DbSet<JournalEntryInstrumentLink> JournalEntryInstrumentLinks => Set<JournalEntryInstrumentLink>();
    public DbSet<PortfolioRule> PortfolioRules => Set<PortfolioRule>();
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePortfolio(modelBuilder);
        ConfigureInstrument(modelBuilder);
        ConfigureTrade(modelBuilder);
        ConfigureCashLedgerEntry(modelBuilder);
        ConfigurePriceSnapshot(modelBuilder);
        ConfigureJournal(modelBuilder);
        ConfigureThesis(modelBuilder);
        ConfigureRules(modelBuilder);
        ConfigureAlerts(modelBuilder);
    }

    private static void ConfigurePortfolio(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.ToTable("portfolios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(IdConverters.PortfolioIdConverter);
            entity.Property(x => x.BaseCurrency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
        });
    }

    private static void ConfigureInstrument(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.ToTable("instruments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(IdConverters.InstrumentIdConverter);
            entity.Property(x => x.PortfolioId).HasConversion(IdConverters.PortfolioIdConverter).IsRequired();
            entity.Property(x => x.Symbol).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Sector).HasMaxLength(120);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasIndex(x => x.Symbol).IsUnique();
        });
    }

    private static void ConfigureTrade(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trade>(entity =>
        {
            entity.ToTable("trades");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(IdConverters.TradeIdConverter);
            entity.Property(x => x.PortfolioId).HasConversion(IdConverters.PortfolioIdConverter).IsRequired();
            entity.Property(x => x.InstrumentId).HasConversion(IdConverters.InstrumentIdConverter).IsRequired();
            entity.Property(x => x.Side).HasConversion<int>().IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("numeric(20,8)").IsRequired();
            entity.Property(x => x.PriceAmount).HasColumnType("numeric(20,4)").IsRequired();
            entity.Property(x => x.FeesAmount).HasColumnType("numeric(20,4)").IsRequired();
            entity.Property(x => x.ExecutedAt).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.CorrectionGroupId).HasConversion(IdConverters.NullableCorrectionGroupIdConverter);
            entity.Property(x => x.CorrectedByTradeId).HasConversion(IdConverters.NullableTradeIdConverter);
            entity.Property(x => x.IsCorrectionReversal).IsRequired();
        });
    }

    private static void ConfigureCashLedgerEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CashLedgerEntry>(entity =>
        {
            entity.ToTable("cash_ledger_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(IdConverters.CashEntryIdConverter);
            entity.Property(x => x.PortfolioId).HasConversion(IdConverters.PortfolioIdConverter).IsRequired();
            entity.Property(x => x.Type).HasConversion<int>().IsRequired();
            entity.Property(x => x.Amount).HasColumnType("numeric(20,4)").IsRequired();
            entity.Property(x => x.OccurredAt).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.CorrectionGroupId).HasConversion(IdConverters.NullableCorrectionGroupIdConverter);
            entity.Property(x => x.CorrectedByCashEntryId).HasConversion(IdConverters.NullableCashEntryIdConverter);
            entity.Property(x => x.IsCorrectionReversal).IsRequired();
        });
    }

    private static void ConfigurePriceSnapshot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceSnapshot>(entity =>
        {
            entity.ToTable("price_snapshots");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(IdConverters.PriceSnapshotIdConverter);
            entity.Property(x => x.InstrumentId).HasConversion(IdConverters.InstrumentIdConverter).IsRequired();
            entity.Property(x => x.Date).IsRequired();
            entity.Property(x => x.ClosePriceAmount).HasColumnType("numeric(20,4)").IsRequired();
            entity.Property(x => x.Source).HasConversion<int>().IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();

            entity.HasIndex(x => new { x.InstrumentId, x.Date }).IsUnique();
        });
    }

    private static void ConfigureJournal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.ToTable("journal_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(IdConverters.JournalEntryIdConverter);
            entity.Property(x => x.PortfolioId).HasConversion(IdConverters.PortfolioIdConverter).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.OccurredAt).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(400).IsRequired();
            entity.Property(x => x.Body).IsRequired();
            entity.Property(x => x.Tags).HasMaxLength(400);
        });

        modelBuilder.Entity<JournalEntryThesisLink>(entity =>
        {
            entity.ToTable("journal_entry_thesis_links");
            entity.HasKey(x => new { x.JournalEntryId, x.ThesisId });
            entity.Property(x => x.JournalEntryId).HasConversion(IdConverters.JournalEntryIdConverter);
            entity.Property(x => x.ThesisId).HasConversion(IdConverters.ThesisIdConverter);
        });

        modelBuilder.Entity<JournalEntryInstrumentLink>(entity =>
        {
            entity.ToTable("journal_entry_instrument_links");
            entity.HasKey(x => new { x.JournalEntryId, x.InstrumentId });
            entity.Property(x => x.JournalEntryId).HasConversion(IdConverters.JournalEntryIdConverter);
            entity.Property(x => x.InstrumentId).HasConversion(IdConverters.InstrumentIdConverter);
        });
    }

    private static void ConfigureThesis(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvestmentThesis>(entity =>
        {
            entity.ToTable("investment_theses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(IdConverters.ThesisIdConverter);
            entity.Property(x => x.PortfolioId).HasConversion(IdConverters.PortfolioIdConverter).IsRequired();
            entity.Property(x => x.InstrumentId).HasConversion(IdConverters.NullableInstrumentIdConverter);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(400).IsRequired();
            entity.Property(x => x.Body).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
        });
    }

    private static void ConfigureRules(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortfolioRule>(entity =>
        {
            entity.ToTable("portfolio_rules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(IdConverters.RuleIdConverter);
            entity.Property(x => x.PortfolioId).HasConversion(IdConverters.PortfolioIdConverter).IsRequired();
            entity.Property(x => x.Type).HasConversion<int>().IsRequired();
            entity.Property(x => x.Enabled).IsRequired();
            entity.Property(x => x.ParametersJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });
    }

    private static void ConfigureAlerts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertEvent>(entity =>
        {
            entity.ToTable("alert_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(IdConverters.AlertEventIdConverter);
            entity.Property(x => x.PortfolioId).HasConversion(IdConverters.PortfolioIdConverter).IsRequired();
            entity.Property(x => x.RuleId).HasConversion(IdConverters.RuleIdConverter).IsRequired();
            entity.Property(x => x.Severity).HasConversion<int>().IsRequired();
            entity.Property(x => x.TriggeredAt).IsRequired();
            entity.Property(x => x.AsOf).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.DetailsJson).HasColumnType("jsonb").IsRequired();
        });
    }
}

public static class IdConverters
{
    public static readonly ValueConverter<PortfolioId, Guid> PortfolioIdConverter = new(v => v.Value, v => new PortfolioId(v));
    public static readonly ValueConverter<InstrumentId, Guid> InstrumentIdConverter = new(v => v.Value, v => new InstrumentId(v));
    public static readonly ValueConverter<TradeId, Guid> TradeIdConverter = new(v => v.Value, v => new TradeId(v));
    public static readonly ValueConverter<CashEntryId, Guid> CashEntryIdConverter = new(v => v.Value, v => new CashEntryId(v));
    public static readonly ValueConverter<PriceSnapshotId, Guid> PriceSnapshotIdConverter = new(v => v.Value, v => new PriceSnapshotId(v));
    public static readonly ValueConverter<JournalEntryId, Guid> JournalEntryIdConverter = new(v => v.Value, v => new JournalEntryId(v));
    public static readonly ValueConverter<ThesisId, Guid> ThesisIdConverter = new(v => v.Value, v => new ThesisId(v));
    public static readonly ValueConverter<RuleId, Guid> RuleIdConverter = new(v => v.Value, v => new RuleId(v));
    public static readonly ValueConverter<AlertEventId, Guid> AlertEventIdConverter = new(v => v.Value, v => new AlertEventId(v));
    public static readonly ValueConverter<CorrectionGroupId?, Guid?> NullableCorrectionGroupIdConverter = new(
        v => v.HasValue ? v.Value.Value : null,
        v => v.HasValue ? new CorrectionGroupId(v.Value) : null);
    public static readonly ValueConverter<TradeId?, Guid?> NullableTradeIdConverter = new(
        v => v.HasValue ? v.Value.Value : null,
        v => v.HasValue ? new TradeId(v.Value) : null);
    public static readonly ValueConverter<CashEntryId?, Guid?> NullableCashEntryIdConverter = new(
        v => v.HasValue ? v.Value.Value : null,
        v => v.HasValue ? new CashEntryId(v.Value) : null);
    public static readonly ValueConverter<InstrumentId?, Guid?> NullableInstrumentIdConverter = new(
        v => v.HasValue ? v.Value.Value : null,
        v => v.HasValue ? new InstrumentId(v.Value) : null);
}

