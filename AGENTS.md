# AGENTS.md

This repository follows Spec-Driven Development using Spec Kit.

## Source of truth order
1. .specify/memory/constitution.md
2. Current feature spec
3. Current feature plan
4. Current feature tasks
5. Existing codebase

## Guardrails
- This is a decision-support platform only.
- Do not add broker execution or auto-trading.
- Do not add leverage, options, or derivatives workflows.
- Build the tracker MVP first.
- Keep scope realistic for a single builder.
- Prefer clear, typed, maintainable code.
- Critical portfolio calculations must be covered by tests.
- Keep API, domain, infrastructure, workers, and UI clearly separated.

## MVP first
The first feature is Portfolio Tracker MVP:
- manual trade entry
- holdings
- cost basis
- realized/unrealized PnL
- allocation %
- cash ledger
- journal/thesis records
- portfolio rules
- alert visibility