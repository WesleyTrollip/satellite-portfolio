using System.Text.Json;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Application;

public sealed record CreateRuleRequest(PortfolioRuleType Type, bool Enabled, string ParametersJson);
public sealed record UpdateRuleRequest(PortfolioRuleType Type, bool Enabled, string ParametersJson);

public sealed class RuleService(
    IPortfolioRuleRepository ruleRepository,
    IPortfolioUnitOfWork unitOfWork)
{
    private static readonly PortfolioId LocalPortfolioId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public Task<IReadOnlyCollection<PortfolioRule>> ListAsync(CancellationToken cancellationToken)
        => ruleRepository.ListAsync(cancellationToken);

    public Task<PortfolioRule?> GetByIdAsync(RuleId ruleId, CancellationToken cancellationToken)
        => ruleRepository.GetByIdAsync(ruleId, cancellationToken);

    public async Task<PortfolioRule> CreateAsync(CreateRuleRequest request, CancellationToken cancellationToken)
    {
        ValidateRuleParameters(request.Type, request.ParametersJson);
        var now = DateTime.UtcNow;
        var rule = new PortfolioRule
        {
            Id = new RuleId(Guid.NewGuid()),
            PortfolioId = LocalPortfolioId,
            Type = request.Type,
            Enabled = request.Enabled,
            ParametersJson = request.ParametersJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        await ruleRepository.AddAsync(rule, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<PortfolioRule?> UpdateAsync(RuleId ruleId, UpdateRuleRequest request, CancellationToken cancellationToken)
    {
        ValidateRuleParameters(request.Type, request.ParametersJson);
        var existing = await ruleRepository.GetByIdAsync(ruleId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new PortfolioRule
        {
            Id = existing.Id,
            PortfolioId = existing.PortfolioId,
            Type = request.Type,
            Enabled = request.Enabled,
            ParametersJson = request.ParametersJson,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        await ruleRepository.UpdateAsync(updated, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return updated;
    }

    private static void ValidateRuleParameters(PortfolioRuleType type, string parametersJson)
    {
        try
        {
            switch (type)
            {
                case PortfolioRuleType.MaxPositionSize:
                {
                    _ = JsonSerializer.Deserialize<MaxPositionSizeParameters>(parametersJson)
                        ?? throw new InvalidOperationException("MaxPositionSize parameters cannot be empty.");
                    break;
                }
                case PortfolioRuleType.MaxSectorConcentration:
                {
                    _ = JsonSerializer.Deserialize<MaxSectorConcentrationParameters>(parametersJson)
                        ?? throw new InvalidOperationException("MaxSectorConcentration parameters cannot be empty.");
                    break;
                }
                case PortfolioRuleType.MaxDrawdown:
                {
                    _ = JsonSerializer.Deserialize<MaxDrawdownParameters>(parametersJson)
                        ?? throw new InvalidOperationException("MaxDrawdown parameters cannot be empty.");
                    break;
                }
                default:
                    throw new InvalidOperationException("Unsupported rule type.");
            }
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Rule parameters are invalid JSON for the selected rule type.", exception);
        }
    }
}

public sealed class AlertService(IAlertEventRepository alertRepository)
{
    public Task<IReadOnlyCollection<AlertEvent>> ListAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
        => alertRepository.ListAsync(from, to, cancellationToken);

    public Task<IReadOnlyCollection<AlertEvent>> ListCurrentAsync(CancellationToken cancellationToken)
        => alertRepository.ListCurrentAsync(cancellationToken);
}

public sealed class RiskEvaluationService(
    IPortfolioRuleRepository rules,
    ITradeRepository trades,
    ICashLedgerRepository cashEntries,
    IPriceSnapshotRepository prices,
    IInstrumentRepository instruments,
    IAlertEventRepository alerts,
    IPortfolioUnitOfWork unitOfWork,
    IHoldingsCalculator holdingsCalculator,
    IPortfolioRuleEvaluator ruleEvaluator)
{
    private static readonly PortfolioId LocalPortfolioId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    public async Task<IReadOnlyCollection<AlertEvent>> EvaluateAndPersistAsync(DateTime asOf, CancellationToken cancellationToken)
    {
        var activeRules = (await rules.ListAsync(cancellationToken)).Where(r => r.Enabled).ToList();
        if (activeRules.Count == 0)
        {
            return [];
        }

        var allTrades = await trades.ListAllAsync(cancellationToken);
        var allCash = await cashEntries.ListAllAsync(cancellationToken);
        var allPrices = await prices.ListAllAsync(cancellationToken);
        var allInstruments = await instruments.ListAsync(cancellationToken);
        var snapshot = holdingsCalculator.CalculateSnapshot(allTrades, allCash, allPrices, asOf);
        var equityCurve = BuildMonthlyEquityCurve(allTrades, allCash, allPrices, asOf);

        var evaluations = ruleEvaluator.EvaluateRules(activeRules, snapshot, allInstruments, equityCurve, asOf)
            .Where(x => x.IsTriggered)
            .ToList();

        if (evaluations.Count == 0)
        {
            return [];
        }

        var alertEvents = evaluations.Select(e => new AlertEvent
        {
            Id = new AlertEventId(Guid.NewGuid()),
            PortfolioId = LocalPortfolioId,
            RuleId = e.RuleId,
            Severity = e.Severity,
            TriggeredAt = DateTime.UtcNow,
            AsOf = asOf,
            Title = e.Title,
            DetailsJson = e.DetailsJson
        }).ToList();

        await alerts.AddRangeAsync(alertEvents, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return alertEvents;
    }

    private IReadOnlyCollection<decimal> BuildMonthlyEquityCurve(
        IReadOnlyCollection<Trade> allTrades,
        IReadOnlyCollection<CashLedgerEntry> allCash,
        IReadOnlyCollection<PriceSnapshot> allPrices,
        DateTime asOf)
    {
        var startDate = allTrades.Select(t => t.ExecutedAt)
            .Concat(allCash.Select(c => c.OccurredAt))
            .DefaultIfEmpty(asOf.AddMonths(-12))
            .Min();

        var curve = new List<decimal>();
        var monthCursor = new DateTime(startDate.Year, startDate.Month, DateTime.DaysInMonth(startDate.Year, startDate.Month), 23, 59, 59, DateTimeKind.Utc);
        while (monthCursor <= asOf)
        {
            var snapshot = holdingsCalculator.CalculateSnapshot(allTrades, allCash, allPrices, monthCursor);
            curve.Add(snapshot.Totals.TotalMarketValue.Amount + snapshot.Totals.CashBalance.Amount);
            monthCursor = monthCursor.AddMonths(1);
        }

        return curve;
    }
}

