using System.Text.Json;

namespace SatellitePortfolio.Domain;

public sealed record RuleEvaluationResult(
    RuleId RuleId,
    bool IsTriggered,
    AlertSeverity Severity,
    string Title,
    string DetailsJson);

public sealed record MaxPositionSizeParameters(decimal MaxPct);
public sealed record MaxSectorConcentrationParameters(decimal MaxPct);
public sealed record MaxDrawdownParameters(decimal MaxDrawdownPct);

public interface IPortfolioRuleEvaluator
{
    IReadOnlyCollection<RuleEvaluationResult> EvaluateRules(
        IReadOnlyCollection<PortfolioRule> rules,
        PortfolioSnapshot snapshot,
        IReadOnlyCollection<Instrument> instruments,
        IReadOnlyCollection<decimal> equityCurve,
        DateTime asOf);
}

public sealed class PortfolioRuleEvaluator : IPortfolioRuleEvaluator
{
    public IReadOnlyCollection<RuleEvaluationResult> EvaluateRules(
        IReadOnlyCollection<PortfolioRule> rules,
        PortfolioSnapshot snapshot,
        IReadOnlyCollection<Instrument> instruments,
        IReadOnlyCollection<decimal> equityCurve,
        DateTime asOf)
    {
        var evaluations = new List<RuleEvaluationResult>();
        var enabledRules = rules.Where(r => r.Enabled).ToList();
        if (enabledRules.Count == 0)
        {
            return evaluations;
        }

        foreach (var rule in enabledRules)
        {
            var result = rule.Type switch
            {
                PortfolioRuleType.MaxPositionSize => EvaluateMaxPositionSize(rule, snapshot, asOf),
                PortfolioRuleType.MaxSectorConcentration => EvaluateMaxSectorConcentration(rule, snapshot, instruments, asOf),
                PortfolioRuleType.MaxDrawdown => EvaluateMaxDrawdown(rule, equityCurve, asOf),
                _ => throw new InvalidOperationException($"Unsupported rule type '{rule.Type}'.")
            };

            evaluations.Add(result);
        }

        return evaluations;
    }

    private static RuleEvaluationResult EvaluateMaxPositionSize(PortfolioRule rule, PortfolioSnapshot snapshot, DateTime asOf)
    {
        var parameters = JsonSerializer.Deserialize<MaxPositionSizeParameters>(rule.ParametersJson)
                         ?? throw new InvalidOperationException("Invalid max position size rule parameters.");

        var candidate = snapshot.Holdings
            .Where(h => h.HasPrice)
            .OrderByDescending(h => h.AllocationPercent)
            .FirstOrDefault();

        var allocation = candidate?.AllocationPercent ?? 0m;
        var triggered = allocation > parameters.MaxPct;
        var details = JsonSerializer.Serialize(new
        {
            measuredPct = allocation,
            limitPct = parameters.MaxPct,
            instrumentId = candidate?.InstrumentId.Value,
            asOf
        });

        return new RuleEvaluationResult(
            rule.Id,
            triggered,
            triggered ? AlertSeverity.Warn : AlertSeverity.Info,
            triggered ? "Max position size exceeded" : "Max position size within threshold",
            details);
    }

    private static RuleEvaluationResult EvaluateMaxSectorConcentration(
        PortfolioRule rule,
        PortfolioSnapshot snapshot,
        IReadOnlyCollection<Instrument> instruments,
        DateTime asOf)
    {
        var parameters = JsonSerializer.Deserialize<MaxSectorConcentrationParameters>(rule.ParametersJson)
                         ?? throw new InvalidOperationException("Invalid max sector concentration rule parameters.");

        var instrumentMap = instruments.ToDictionary(i => i.Id, i => i);
        var sectorPct = snapshot.Holdings
            .Where(h => h.HasPrice && instrumentMap.TryGetValue(h.InstrumentId, out _))
            .GroupBy(h =>
            {
                var instrument = instrumentMap[h.InstrumentId];
                return string.IsNullOrWhiteSpace(instrument.Sector) ? "Unknown" : instrument.Sector!;
            })
            .Select(g => new { Sector = g.Key, AllocationPct = g.Sum(x => x.AllocationPercent) })
            .OrderByDescending(x => x.AllocationPct)
            .FirstOrDefault();

        var measured = sectorPct?.AllocationPct ?? 0m;
        var triggered = measured > parameters.MaxPct;
        var details = JsonSerializer.Serialize(new
        {
            measuredPct = measured,
            limitPct = parameters.MaxPct,
            sector = sectorPct?.Sector,
            asOf
        });

        return new RuleEvaluationResult(
            rule.Id,
            triggered,
            triggered ? AlertSeverity.Warn : AlertSeverity.Info,
            triggered ? "Max sector concentration exceeded" : "Max sector concentration within threshold",
            details);
    }

    private static RuleEvaluationResult EvaluateMaxDrawdown(PortfolioRule rule, IReadOnlyCollection<decimal> equityCurve, DateTime asOf)
    {
        var parameters = JsonSerializer.Deserialize<MaxDrawdownParameters>(rule.ParametersJson)
                         ?? throw new InvalidOperationException("Invalid max drawdown rule parameters.");

        var drawdown = ComputeDrawdown(equityCurve);
        var triggered = drawdown > parameters.MaxDrawdownPct;
        var details = JsonSerializer.Serialize(new
        {
            measuredDrawdownPct = drawdown,
            limitPct = parameters.MaxDrawdownPct,
            asOf
        });

        return new RuleEvaluationResult(
            rule.Id,
            triggered,
            triggered ? AlertSeverity.Critical : AlertSeverity.Info,
            triggered ? "Max drawdown threshold breached" : "Max drawdown within threshold",
            details);
    }

    public static decimal ComputeDrawdown(IReadOnlyCollection<decimal> equityCurve)
    {
        if (equityCurve.Count == 0)
        {
            return 0m;
        }

        decimal peak = decimal.MinValue;
        decimal maxDrawdown = 0m;
        foreach (var value in equityCurve)
        {
            if (value > peak)
            {
                peak = value;
            }

            if (peak <= 0m)
            {
                continue;
            }

            var drawdown = (peak - value) / peak;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }
        }

        return maxDrawdown;
    }
}

