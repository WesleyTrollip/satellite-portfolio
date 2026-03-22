using System.Text.Json;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Domain.Tests;

public class RuleEvaluatorTests
{
    private readonly PortfolioRuleEvaluator _evaluator = new();

    [Fact]
    public void MaxPositionSizeRule_Triggers_WhenAllocationExceedsThreshold()
    {
        var rule = new PortfolioRule
        {
            Id = new RuleId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Type = PortfolioRuleType.MaxPositionSize,
            Enabled = true,
            ParametersJson = JsonSerializer.Serialize(new MaxPositionSizeParameters(0.10m)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var snapshot = new PortfolioSnapshot(
            [
                new Holding(
                    new InstrumentId(Guid.NewGuid()),
                    10m,
                    new Money(100m),
                    new Money(1_000m),
                    new Money(0m),
                    0.4m,
                    true)
            ],
            new PortfolioTotals(new Money(1_000m), new Money(1_000m), new Money(0m), new Money(0m), new Money(1_500m)));

        var result = _evaluator.EvaluateRules([rule], snapshot, [], [2_500m], DateTime.UtcNow).Single();
        Assert.True(result.IsTriggered);
    }

    [Fact]
    public void MaxSectorConcentrationRule_HandlesMissingPrice_ByNotTriggeringOnUnknown()
    {
        var instrumentId = new InstrumentId(Guid.NewGuid());
        var rule = new PortfolioRule
        {
            Id = new RuleId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Type = PortfolioRuleType.MaxSectorConcentration,
            Enabled = true,
            ParametersJson = JsonSerializer.Serialize(new MaxSectorConcentrationParameters(0.2m)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var snapshot = new PortfolioSnapshot(
            [
                new Holding(instrumentId, 5m, new Money(100m), new Money(0m), new Money(0m), 0m, false)
            ],
            new PortfolioTotals(new Money(0m), new Money(500m), new Money(0m), new Money(0m), new Money(500m)));

        var instruments = new[]
        {
            new Instrument
            {
                Id = instrumentId,
                PortfolioId = new PortfolioId(Guid.NewGuid()),
                Symbol = "AAPL",
                Sector = "Technology",
                Currency = "EUR",
                CreatedAt = DateTime.UtcNow
            }
        };

        var result = _evaluator.EvaluateRules([rule], snapshot, instruments, [500m], DateTime.UtcNow).Single();
        Assert.False(result.IsTriggered);
    }

    [Fact]
    public void MaxDrawdownRule_Triggers_WhenDrawdownBreachesThreshold()
    {
        var rule = new PortfolioRule
        {
            Id = new RuleId(Guid.NewGuid()),
            PortfolioId = new PortfolioId(Guid.NewGuid()),
            Type = PortfolioRuleType.MaxDrawdown,
            Enabled = true,
            ParametersJson = JsonSerializer.Serialize(new MaxDrawdownParameters(0.15m)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var curve = new[] { 100m, 120m, 90m, 80m };
        var result = _evaluator.EvaluateRules([rule], new PortfolioSnapshot([], new PortfolioTotals(new Money(0), new Money(0), new Money(0), new Money(0), new Money(0))), [], curve, DateTime.UtcNow).Single();
        Assert.True(result.IsTriggered);
    }

    [Fact]
    public void ComputeDrawdown_ReturnsExpectedValue_ForSyntheticCurve()
    {
        var drawdown = PortfolioRuleEvaluator.ComputeDrawdown([100m, 120m, 108m, 90m, 95m]);
        Assert.Equal(0.25m, drawdown);
    }
}

