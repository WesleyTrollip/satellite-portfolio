# Feature Specification: Satellite Portfolio Tracker MVP

**Feature Branch**: `001-portfolio-tracker-mvp`  
**Created**: 2026-03-17  
**Status**: Draft  
**Input**: Build a single-user satellite portfolio tracker for a €30,000 learning and experimentation portfolio. The system supports manual recording of buys, sells, cash movements, holdings, cost basis, realized and unrealized PnL, allocation percentages, and journal-linked investment theses. The tracker supports portfolio rules and risk visibility (position size, sector concentration, drawdown alert thresholds). Decision support only; no automatic trade placement. The user can review current positions, historical trades, cash state, and monthly portfolio state in a clean dashboard. MVP foundation for a future AI-assisted decision-support platform.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record trades & cash movements (Priority: P1)

As a single user, I can manually record buys, sells, and cash movements so that the portfolio state can be reconstructed and audited.

**Why this priority**: Without accurate inputs, every downstream calculation (holdings, cost basis, PnL, allocation, alerts) is unreliable.

**Independent Test**: Create a new portfolio, record a sequence of cash deposit → buy → partial sell → withdrawal, and verify the resulting trade ledger and cash ledger reflect the entries and can be reviewed.

**Acceptance Scenarios**:

1. **Given** an empty portfolio, **When** I record a cash deposit of €10,000, **Then** cash balance increases by €10,000 and an auditable cash ledger entry exists.
2. **Given** sufficient cash, **When** I record a buy of 10 shares at €100 with fees, **Then** holdings increase, cash decreases by the total outflow, and a trade record exists with timestamp and notes.
3. **Given** an existing holding, **When** I record a sell of 4 shares at €120 with fees, **Then** holdings decrease by 4, realized PnL is computed, cash increases by proceeds, and the trade is visible in history.

---

### User Story 2 - Review current positions & portfolio snapshot (Priority: P1)

As a user, I can review current holdings, cost basis, realized/unrealized PnL, allocation %, and cash in a clean dashboard to understand my portfolio at a glance.

**Why this priority**: The dashboard is the primary review workflow and must be accurate and clear.

**Independent Test**: Seed a small set of trades and prices, open the dashboard, and verify the displayed positions and portfolio totals match expected values.

**Acceptance Scenarios**:

1. **Given** recorded trades and current prices, **When** I view the dashboard, **Then** I see a positions table showing quantity, average cost basis, market value, unrealized PnL, and allocation % per position.
2. **Given** recorded sells, **When** I view the dashboard, **Then** realized PnL is shown and reconciles to the trade history.
3. **Given** cash movements, **When** I view the dashboard, **Then** cash state (balance and recent ledger) is visible and matches the ledger.

---

### User Story 3 - Inspect history & monthly state (Priority: P2)

As a user, I can review historical trades and see a monthly portfolio state snapshot to support learning, journaling, and reconciliation.

**Why this priority**: History and monthly snapshots enable auditability, review, and learning loops without requiring external spreadsheets.

**Independent Test**: Record trades across two calendar months and verify monthly snapshots can be generated and viewed for each month.

**Acceptance Scenarios**:

1. **Given** trades over time, **When** I open the trade history view, **Then** I can filter/sort and see each trade with date, symbol, quantity, price, fees, and notes.
2. **Given** trades across months, **When** I view the monthly state for a month-end, **Then** I see holdings, cash, and portfolio totals as of that month-end.

---

### User Story 4 - Journal-linked investment theses (Priority: P2)

As a user, I can create journal/thesis entries and link them to a position so that my decisions are documented alongside portfolio outcomes.

**Why this priority**: A learning portfolio needs a tight feedback loop between thesis and results.

**Independent Test**: Create a thesis entry, link it to a symbol/position, and verify it appears in the position details and can be reviewed later.

**Acceptance Scenarios**:

1. **Given** an existing position, **When** I add a thesis entry and link it to that position, **Then** the position details show the linked thesis and timestamps.

---

### User Story 5 - Rules & risk visibility (Priority: P3)

As a user, I can define portfolio rules and see risk visibility/alerts for position size, sector concentration, and drawdown thresholds.

**Why this priority**: The platform’s “decision support” value starts with clear, explicit risk visibility.

**Independent Test**: Define a position size limit, assign sectors to holdings, and verify the dashboard highlights violations and alerts without mutating state.

**Acceptance Scenarios**:

1. **Given** a max position size rule (e.g., 10%), **When** a position exceeds the threshold, **Then** an alert is shown with explanation and current value vs limit.
2. **Given** a max sector concentration rule, **When** combined sector allocation exceeds the threshold, **Then** an alert is shown and the sector breakdown is visible.
3. **Given** a drawdown alert threshold, **When** portfolio drawdown exceeds the threshold, **Then** an alert is shown with the computed drawdown and reference peak.

### Edge Cases

- Missing/unknown price for a holding: calculations should degrade gracefully (e.g., mark market value as unavailable) without corrupting holdings/cost basis.
- Selling more than current quantity: the system must prevent the action or require explicit correction (no silent negative holdings).
- Fees/taxes and cash movements that would drive cash negative: behavior must be explicit (reject or allow with clear warning, per later spec/plan).
- Corporate actions (splits, dividends): out of scope for MVP unless explicitly added later; must not be “partially supported” silently.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST be single-user and local-first friendly (no multi-tenant assumptions in MVP).
- **FR-002**: System MUST support manual entry of trades: buy and sell, with date/time, symbol, quantity, price, fees, and optional notes.
- **FR-003**: System MUST support manual entry of cash movements (deposit/withdrawal/adjustment) with date/time, amount, and notes.
- **FR-004**: System MUST compute holdings per symbol from recorded trades.
- **FR-005**: System MUST compute cost basis per symbol using an explicit, documented method (default: average cost or FIFO; choose one in plan and cover with tests).
- **FR-006**: System MUST compute realized PnL for sells and unrealized PnL from current prices.
- **FR-007**: System MUST compute allocation percentages by position and (if sector is provided) by sector.
- **FR-008**: System MUST provide a dashboard showing: positions, cash state, portfolio totals, realized/unrealized PnL, and allocation %.
- **FR-009**: System MUST provide a trade history view that is filterable/sortable.
- **FR-010**: System MUST support monthly portfolio state snapshots viewable by month (as-of month-end).
- **FR-011**: System MUST support journal/thesis records and linking them to a position/symbol.
- **FR-012**: System MUST support portfolio rules for (at minimum) position size, sector concentration, and drawdown alert thresholds.
- **FR-013**: System MUST display rule evaluations/alerts as decision-support visibility (no auto-remediation).
- **FR-014**: System MUST maintain auditability: every portfolio-affecting entry (trade/cash movement/edit/delete) must be recorded with timestamps and before/after values.
- **FR-015**: System MUST NOT place trades, submit orders, or integrate broker execution in MVP.
- **FR-016**: Any AI/LLM capability, if present, MUST be advisory only and MUST NOT mutate portfolio state without explicit user action.

### Key Entities *(include if feature involves data)*

- **Portfolio**: Single user’s portfolio; base currency EUR; configuration for rules and reporting periods.
- **Trade**: Buy/Sell; timestamp; symbol; quantity; unit price; fees; notes; links to journal entries.
- **CashLedgerEntry**: Deposit/Withdrawal/Adjustment; timestamp; amount; notes; audit metadata.
- **Holding**: Derived state per symbol (quantity, cost basis, realized/unrealized PnL, allocation %).
- **PricePoint**: Symbol; timestamp; price in EUR (source may be manual or imported later).
- **JournalEntry (Thesis)**: Timestamped notes; optional tags; links to symbol/position; may reference trades.
- **Rule**: Position size, sector concentration, drawdown threshold; parameters; enabled/disabled; explanation text.
- **Alert**: Derived evaluation result for a rule at a point in time, with explanation and measured values.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can record a deposit, a buy, and a sell end-to-end, and the dashboard reflects updated holdings and cash within one refresh.
- **SC-002**: For a provided set of deterministic test cases, holdings, cost basis, realized PnL, and allocation % match expected outputs (100% pass).
- **SC-003**: The system can generate and display monthly portfolio state snapshots for at least 12 months of activity without manual data export.
- **SC-004**: Every trade and cash movement is viewable in history with sufficient detail to reconcile portfolio totals (audit trail is complete).
