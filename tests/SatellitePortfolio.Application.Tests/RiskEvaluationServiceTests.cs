using System.Text.Json;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application.Tests;

public class RiskEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateAndPersist_WritesAlertEvents_WhenRuleTriggers()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var trades = new InMemoryTradeRepository();
        var cash = new InMemoryCashLedgerRepository();
        var prices = new InMemoryPriceSnapshotRepository();
        var instruments = new InMemoryInstrumentRepository();
        var alerts = new InMemoryAlertEventRepository();
        var rules = new InMemoryRuleRepository();

        instruments.Items.Add(new Instrument
        {
            Id = instrumentId,
            PortfolioId = new PortfolioId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            Symbol = "AAPL",
            Sector = "Technology",
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow
        });

        cash.Items.Add(new CashLedgerEntry
        {
            Id = new CashEntryId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Type = CashEntryType.Deposit,
            Amount = 5_000m,
            OccurredAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        });

        trades.Items.Add(new Trade
        {
            Id = new TradeId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            InstrumentId = instrumentId,
            Side = TradeSide.Buy,
            Quantity = 10m,
            PriceAmount = 200m,
            FeesAmount = 0m,
            ExecutedAt = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        });

        prices.Items.Add(new PriceSnapshot
        {
            Id = new PriceSnapshotId(Guid.NewGuid()),
            InstrumentId = instrumentId,
            Date = new DateOnly(2026, 1, 2),
            ClosePriceAmount = 210m,
            Source = PriceSnapshotSource.Manual,
            CreatedAt = DateTime.UtcNow
        });

        rules.Items.Add(new PortfolioRule
        {
            Id = new RuleId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
            Type = PortfolioRuleType.MaxPositionSize,
            Enabled = true,
            ParametersJson = JsonSerializer.Serialize(new MaxPositionSizeParameters(0.2m)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var service = new RiskEvaluationService(
            rules,
            trades,
            cash,
            prices,
            instruments,
            alerts,
            new InMemoryUnitOfWork(),
            new HoldingsCalculator(),
            new PortfolioRuleEvaluator());

        var created = await service.EvaluateAndPersistAsync(new DateTime(2026, 1, 2, 22, 0, 0, DateTimeKind.Utc), CancellationToken.None);
        Assert.Single(created);
        Assert.Single(alerts.Items);
    }
}

internal sealed class InMemoryRuleRepository : IPortfolioRuleRepository
{
    public List<PortfolioRule> Items { get; } = [];

    public Task<IReadOnlyCollection<PortfolioRule>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<PortfolioRule>>(Items.ToList());

    public Task<PortfolioRule?> GetByIdAsync(RuleId ruleId, CancellationToken cancellationToken)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == ruleId));

    public Task AddAsync(PortfolioRule rule, CancellationToken cancellationToken)
    {
        Items.Add(rule);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PortfolioRule rule, CancellationToken cancellationToken)
    {
        var index = Items.FindIndex(x => x.Id == rule.Id);
        if (index >= 0)
        {
            Items[index] = rule;
        }

        return Task.CompletedTask;
    }
}

