# Research Notes: Portfolio Tracker MVP

**Feature**: `001-portfolio-tracker-mvp`  
**Date**: 2026-03-17  
**Purpose**: Record design decisions and trade-offs needed before implementation.

## Decisions (to lock in for MVP)

### 1) Cost basis method

**Recommendation (MVP)**: **Average cost** for simplicity and strong testability.

- Pros: simpler data model; easier corrections; fewer edge cases.
- Cons: less aligned with tax-lot reporting (out of scope for MVP).

**Alternative**: FIFO lots (defer unless you explicitly need lot-level audit sooner).

### 2) Editing incorrect trades and cash entries (auditability)

**Recommendation (MVP)**: **Reversal + replacement**.

Model:
- Keep the original immutable entry.
- Create a reversal entry (equal and opposite economic effect).
- Create a replacement entry with corrected details.
- Link all three via `CorrectionGroupId` (or similar) and mark the original as corrected.

Pros:
- Ledger stays append-only; recomputation is deterministic.
- UI can show “what happened” clearly.

Alternative: versioned records (also valid, but more complex to query).

### 3) End-of-day price snapshots (no streaming)

**MVP stance**:
- Store `PriceSnapshot(symbol, date, closePriceEur, source, createdAt)`.
- Use “last available on/before date” for month-end views.
- Missing prices must not break holdings/cost basis; valuation-based metrics become partially unavailable with explicit messaging.

### 4) Auth later, not now

MVP runs locally without auth. To keep the path open:
- Introduce an `ICurrentUser`/`IUserContext` abstraction in API/Application but default to a constant “local user”.
- Keep `PortfolioId` explicit in storage even if only one exists (helps later multi-portfolio/user expansion without rewriting domain logic).

## Open Questions (for the plan to decide explicitly)

- Should cash be included in “total portfolio value” for allocation and rules? (Default: **yes**.)
- How to handle negative cash? (Default: disallow by validation, or allow with explicit warning—choose in implementation.)

