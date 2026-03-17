# Satellite Portfolio Tracker Constitution

This repository builds a single-user satellite portfolio tracker and decision-support platform.

## Core Principles

### 1. Decision support only
No auto-trading, broker execution, or any workflow that can mutate real brokerage accounts. LLM features are advisory only and must never mutate portfolio state without explicit user action.

### 2. Build incrementally (MVP first)
Ship the tracker MVP first and iterate. Avoid speculative platform features and keep scope realistic for a single builder.

### 3. Accuracy over flash
Cost basis, realized/unrealized PnL, allocation %, and auditability are critical. Prefer correctness and clear review workflows over visual complexity.

### 4. Explicit, testable domain rules
Every domain rule must be explicit, deterministic, and testable. Avoid hidden heuristics or “magic” behavior in calculations and rule evaluation.

### 5. UI optimized for clarity and review
The UI should optimize for inspection, reconciliation, and review workflows (entries, diffs, history, explanations), not dashboards for their own sake.

### 6. Boring tech, strong typing
Prefer boring, maintainable technology and strong typing. Optimize for readability, refactorability, and low cognitive overhead.

### 7. Tests required for critical logic
All critical calculations and domain rules require tests, especially: cost basis, holdings, PnL, allocation %, cash ledger integrity, and rule evaluation.

### 8. Auditability of portfolio changes
All portfolio changes must be auditable: who/what changed, when, why (optional notes), and before/after values where applicable. No silent mutation.

### 9. Clean architecture boundaries
Preserve clear separation between API, domain logic, infrastructure, workers, and UI. Domain logic must not depend on UI or infrastructure.

### 10. Advisory LLM features only
LLM features may summarize, explain, and suggest—but must not directly change portfolio state. Any proposed change must be surfaced as a user reviewable action.

### 11. Single-builder productivity
Optimize for a single builder: simple workflows, few moving parts, fast feedback loops, and pragmatic tooling.

### 12. Flexible deployment
Keep deployment flexible: local-first, VPS-friendly, and easy to run with minimal external dependencies.

## Constraints and Non-Goals

- **No auto-trading / execution**: Do not add broker integrations that execute trades or place orders.
- **No leverage/options/derivatives workflows**: Avoid leverage, options, and derivatives flows unless explicitly specified later.
- **MVP focus**: Manual trade entry, holdings, cost basis, realized/unrealized PnL, allocation %, cash ledger, journal/thesis records, portfolio rules, and alert visibility.

## Governance

This constitution is the source of truth for product and engineering decisions. If a spec, plan, task list, or implementation conflicts with these principles, the conflicting work must be revised to comply.

Amendments must:
- State the motivation and intended impact.
- Update relevant specs/plans and include a migration plan if behavior changes.

**Version**: 1.0.0 | **Ratified**: 2026-03-17 | **Last Amended**: 2026-03-17
