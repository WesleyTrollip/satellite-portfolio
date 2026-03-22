# REST Endpoints: Portfolio Tracker MVP

Conventions:
- Base URL: `/api`
- All write operations are explicit user actions (no automated trading).
- For MVP, auth is not required; keep future auth in mind (e.g., `PortfolioId` stays explicit internally).
- For corrections, prefer **POST correction** endpoints rather than silent PUT overwrites.

## Portfolio

- `GET /api/portfolio/overview`
  - Returns totals, cash, realized/unrealized PnL, allocation summary, and current alerts.
- `GET /api/portfolio/monthly?year=YYYY&month=MM`
  - Returns month-end portfolio state (holdings + cash + totals) with missing-price explanations if applicable.

## Instruments

- `GET /api/instruments`
- `POST /api/instruments`
- `GET /api/instruments/{instrumentId}`
- `PUT /api/instruments/{instrumentId}`

## Trades (ledger)

- `GET /api/trades?from=...&to=...&instrumentId=...`
- `POST /api/trades`
- `GET /api/trades/{tradeId}`

### Trade correction (auditable edit)

- `POST /api/trades/{tradeId}/corrections`
  - Body includes the corrected trade fields + optional reason.
  - Server records reversal + replacement (or versioned edit) and returns the updated set.

## Cash ledger

- `GET /api/cash/entries?from=...&to=...`
- `POST /api/cash/entries`
- `GET /api/cash/entries/{cashEntryId}`

### Cash entry correction (auditable edit)

- `POST /api/cash/entries/{cashEntryId}/corrections`

## Positions / Holdings (derived, read-only)

- `GET /api/holdings`
  - Returns per-instrument holdings view (qty, cost basis, market value, allocation %, unrealized PnL) with missing-price flags.
- `GET /api/holdings/{instrumentId}`

## Price snapshots (EOD)

- `GET /api/prices/snapshots?instrumentId=...&from=...&to=...`
- `POST /api/prices/snapshots`
  - Add/update an EOD close for a date (ensure idempotency rules are explicit in implementation).

## Journal / Theses

- `GET /api/journal`
- `POST /api/journal`
- `GET /api/journal/{journalEntryId}`
- `PUT /api/journal/{journalEntryId}`

- `GET /api/theses`
- `POST /api/theses`
- `GET /api/theses/{thesisId}`
- `PUT /api/theses/{thesisId}`

## Rules & Alerts

- `GET /api/rules`
- `POST /api/rules`
- `GET /api/rules/{ruleId}`
- `PUT /api/rules/{ruleId}`

- `GET /api/alerts?from=...&to=...`
- `GET /api/alerts/current`

