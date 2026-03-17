# Data Model: Portfolio Tracker MVP

**Feature**: `001-portfolio-tracker-mvp`  
**Date**: 2026-03-17  
**Database**: PostgreSQL (EF Core + Npgsql)  
**Scope**: Canonical ledger + minimal supporting entities for derived views, journaling, rules, and alerts.

## Guiding constraints

- **Single user, one portfolio** in MVP, but identifiers should still be explicit to keep future auth/multi-portfolio possible.
- **Auditability**: portfolio state is reconstructable from immutable events (trades + cash ledger + corrections).
- **No broker integrations, no auto-trading**.
- **No streaming prices**: EOD snapshots only.

## Core entities (conceptual)

### Portfolio

- `PortfolioId` (UUID)
- `BaseCurrency` = EUR
- `CreatedAt`

Notes: MVP can initialize exactly one portfolio row.

### Instrument

- `InstrumentId` (UUID)
- `Symbol` (string, unique)
- `Name` (string, optional)
- `Sector` (string, optional; used for concentration rules)
- `Currency` (string; default EUR in MVP)
- `CreatedAt`

### Trade (ledger event)

Immutable economic event.

- `TradeId` (UUID)
- `PortfolioId`
- `InstrumentId`
- `Side` (Buy|Sell)
- `Quantity` (decimal, > 0)
- `Price` (decimal, >= 0) in EUR
- `Fees` (decimal, >= 0) in EUR
- `ExecutedAt` (timestamp)
- `Notes` (string, optional)
- `CreatedAt`
- **Correction fields**
  - `CorrectionGroupId` (UUID, nullable)
  - `CorrectedByTradeId` (UUID, nullable)  # points to replacement trade (optional)
  - `IsCorrectionReversal` (bool)          # true for reversal entry

MVP correction approach: reversal + replacement (see `clarifications.md`).

### CashLedgerEntry (ledger event)

- `CashEntryId` (UUID)
- `PortfolioId`
- `Type` (Deposit|Withdrawal|Adjustment)
- `Amount` (decimal; deposits positive, withdrawals negative; adjustments either)
- `OccurredAt` (timestamp)
- `Notes` (string, optional)
- `CreatedAt`
- **Correction fields** (same pattern as trades)
  - `CorrectionGroupId` (UUID, nullable)
  - `CorrectedByCashEntryId` (UUID, nullable)
  - `IsCorrectionReversal` (bool)

### PriceSnapshot (EOD)

- `PriceSnapshotId` (UUID)
- `InstrumentId`
- `Date` (date, unique per instrument)
- `ClosePrice` (decimal, >= 0) in EUR
- `Source` (Manual|Import|Other)
- `CreatedAt`

### JournalEntry

- `JournalEntryId` (UUID)
- `PortfolioId`
- `CreatedAt`
- `OccurredAt` (timestamp; user-entered)
- `Title` (string)
- `Body` (markdown/text)
- `Tags` (optional)

### InvestmentThesis

- `ThesisId` (UUID)
- `PortfolioId`
- `InstrumentId` (nullable; can be linked to a symbol)
- `CreatedAt`
- `UpdatedAt`
- `Title` (string)
- `Body` (markdown/text)
- `Status` (Active|Retired)

Linking:

- `JournalEntryThesisLink` (optional join) if you want many-to-many between journal and theses.
- `JournalEntryInstrumentLink` for linking notes to instruments without being a thesis.

### PortfolioRule

- `RuleId` (UUID)
- `PortfolioId`
- `Type` (MaxPositionSize|MaxSectorConcentration|MaxDrawdown)
- `Enabled` (bool)
- `ParametersJson` (jsonb)  # e.g., { "maxPct": 0.10 }
- `CreatedAt`
- `UpdatedAt`

### AlertEvent (derived, immutable)

- `AlertEventId` (UUID)
- `PortfolioId`
- `RuleId`
- `Severity` (Info|Warn|Critical)
- `TriggeredAt` (timestamp)
- `AsOf` (timestamp/date for the evaluation)
- `Title` (string)
- `DetailsJson` (jsonb)  # measured values, thresholds, explanation

## Derived views (not canonical)

Derived read models should be computed from the ledger + snapshots:

- **Holding/Position view** per instrument: quantity, avg cost (if average cost), market value (if price exists), unrealized PnL, allocation %.
- **Cash view**: sum of cash entries + trade cash flows.
- **Portfolio totals**: sum of market values + cash, realized/unrealized totals.
- **Month-end state**: as-of month-end using “last EOD price on/before date”.

MVP can compute on demand (queries) and optionally cache summaries later.

## Invariants and validation rules (must be tested)

- Quantity cannot go negative via sells (unless a future “shorting” feature explicitly added; out of scope).
- Trades and cash entries are immutable; corrections must preserve full history.
- Allocation and rule evaluation must degrade gracefully with missing prices (no silent assumptions).

