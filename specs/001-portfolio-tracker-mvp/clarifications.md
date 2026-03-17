# Clarifications: Portfolio Tracker MVP

**Feature**: `specs/001-portfolio-tracker-mvp/spec.md`  
**Date**: 2026-03-17  
**Scope**: MVP boundaries, portfolio rules semantics, and data model assumptions.

## MVP Boundaries (what’s in vs out)

### In scope (MVP)

- **Manual entry**: buys, sells, and cash movements (deposit/withdrawal/adjustment), each with timestamp and notes.
- **Editing/correction workflow**: editing incorrect trades/cash entries is supported via an auditable correction mechanism (see Auditability assumptions below).
- **Derived portfolio state**: holdings, cost basis, realized/unrealized PnL, allocation %, cash balance.
- **Review UX**: clean dashboard for current positions + totals, trade history, cash ledger, and monthly portfolio state.
- **Journaling**: thesis/journal entries linked to symbol/position (and optionally to trades).
- **Rules & visibility**: position size, sector concentration, drawdown thresholds surfaced as alerts/explanations.
- **Auditability**: portfolio state must be reconstructable from an append-only event/ledger history (or an equivalent auditable mechanism).

### Explicitly out of scope (MVP)

- **Broker integrations and execution**: no order placement, no “connect to broker and sync”, no auto-trading.
- **Derivatives / leverage workflows**: options, futures, margin/leverage features are out of scope.
- **Corporate actions**: splits, dividends, spin-offs, symbol changes (unless specified later); avoid partial/implicit support.
- **Tax lots / tax reporting**: beyond the chosen cost basis method’s requirements; no country-specific tax filing features.
- **Tax engine**: no tax calculations, tax forms, or jurisdiction-specific tax treatment logic.
- **Multi-user / sharing**: no accounts, auth, permissions, collaboration.
- **Complex performance analytics**: factor attribution, benchmarking, IRR/MWRR/TWRR (unless specified later).
- **Real-time streaming prices**: no websockets/streaming feeds; end-of-day snapshots are acceptable.

## Portfolio Rules (semantics and assumptions)

Rules are **decision-support visibility only**. They can create **alerts** and **explanations**, but must not mutate portfolio state.

### Rule evaluation timing

- **Default**: evaluate rules on-demand when viewing the dashboard and after any portfolio-affecting event is recorded.
- **Optional later**: background evaluation/notifications (worker) may be added, but still advisory only.

### Position size rule

- **Definition**: position size is measured as \( \frac{\text{position market value}}{\text{total portfolio market value}} \).
- **Portfolio market value**: sum of market values of all positions + cash (cash included unless a later plan specifies exclusion).
- **Trigger**: alert when size exceeds configured threshold.

### Sector concentration rule

- **Sector source**: user-assigned per symbol (simple mapping in MVP).
- **Definition**: sector concentration is \( \frac{\sum \text{market value of positions in sector}}{\text{total portfolio market value}} \).
- **Trigger**: alert when any sector exceeds configured threshold.

### Drawdown rule

- **Definition**: drawdown computed from **portfolio equity curve** \(E(t)\) where \(E(t)\) is total portfolio market value (positions + cash) at time \(t\).
- **Reference peak**: max \(E(t)\) over a configured lookback (default: “since inception” unless plan chooses otherwise).
- **Drawdown %**: \( \frac{E(t)-\max(E)}{\max(E)} \) (negative values).
- **Trigger**: alert when drawdown magnitude exceeds threshold.

### Missing data behavior (rules)

- If **current prices are missing** for a holding, the system must:
  - keep holdings/cost basis correct,
  - mark market-value-based metrics (allocation, unrealized PnL, rules) as **partially unavailable**,
  - and show an explanation (“price missing for X; allocation and rules exclude/flag this position”).

## Data Model Assumptions (MVP)

### Currency and valuation

- **Base currency**: EUR (€, single-currency MVP).
- **Price currency**: assume prices are in EUR. FX conversion is out of scope for MVP unless specified later.

### Pricing

- **Price cadence**: end-of-day snapshots are acceptable in MVP; no requirement for intraday/streaming updates.
- **Current price input**: may be manual initially (or imported later), but must be explicit where prices come from.
- **No silent pricing**: do not infer prices from trades as “current price”.

### Cost basis method (must be chosen in plan)

The plan must pick **one explicit method** and cover it with tests:

- **Average cost (recommended for MVP simplicity)**, or
- **FIFO lots** (more complex; may be needed later for audit/tax-lot reporting).

Regardless of method:
- Sells must compute realized PnL deterministically.
- Prevent or explicitly handle sells that exceed holdings (no silent negative positions).

### Ledger / audit trail approach

- Portfolio-affecting events are **Trade** and **CashLedgerEntry** (and any edits/deletes must be represented as auditable events, not silent overwrites).
- The system must be able to reconstruct holdings and cash from the ledger.

#### Editing incorrect trades (required)

Edits must preserve auditability. MVP should implement one of these patterns (choose in plan and enforce consistently):

- **Reversal + replacement (preferred)**: mark the original entry as corrected, add an explicit reversing entry, then add a corrected replacement entry; keep links between the entries.
- **Versioned records**: keep immutable versions of each entry; editing creates a new version; derived state uses “latest active version” with full history preserved.

In all cases:
- The UI must show that an entry was edited/corrected and why (optional note).
- Derived holdings/cash must recompute from the auditable history deterministically.

### Monthly portfolio state snapshots

- **Meaning**: an “as-of” view for month-end.
- **Computation**: derived from ledger and prices as of the month-end timestamp (prices may be missing → snapshot shows partial valuation with explanation).

### Identity and uniqueness

- **Symbol**: primary identifier for a holding in MVP (no multi-venue identifiers).
- **Timestamps**: stored with timezone (or UTC) consistently; sorting and month-end boundaries must be well-defined.

