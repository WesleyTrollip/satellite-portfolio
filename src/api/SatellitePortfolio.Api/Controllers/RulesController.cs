using Microsoft.AspNetCore.Mvc;
using SatellitePortfolio.Application;
using SatellitePortfolio.Domain;

namespace SatellitePortfolio.Api.Controllers;

[ApiController]
[Route("api/rules")]
public sealed class RulesController(RuleService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PortfolioRule>>> List(CancellationToken cancellationToken)
    {
        var rules = await service.ListAsync(cancellationToken);
        return Ok(rules);
    }

    [HttpGet("{ruleId:guid}")]
    public async Task<ActionResult<PortfolioRule>> GetById(Guid ruleId, CancellationToken cancellationToken)
    {
        var rule = await service.GetByIdAsync(new RuleId(ruleId), cancellationToken);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpPost]
    public async Task<ActionResult<PortfolioRule>> Create([FromBody] CreateRuleDto request, CancellationToken cancellationToken)
    {
        var rule = await service.CreateAsync(
            new CreateRuleRequest(request.Type, request.Enabled, request.ParametersJson),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { ruleId = rule.Id.Value }, rule);
    }

    [HttpPut("{ruleId:guid}")]
    public async Task<ActionResult<PortfolioRule>> Update(
        Guid ruleId,
        [FromBody] UpdateRuleDto request,
        CancellationToken cancellationToken)
    {
        var rule = await service.UpdateAsync(
            new RuleId(ruleId),
            new UpdateRuleRequest(request.Type, request.Enabled, request.ParametersJson),
            cancellationToken);

        return rule is null ? NotFound() : Ok(rule);
    }
}

public sealed record CreateRuleDto(PortfolioRuleType Type, bool Enabled, string ParametersJson);
public sealed record UpdateRuleDto(PortfolioRuleType Type, bool Enabled, string ParametersJson);

