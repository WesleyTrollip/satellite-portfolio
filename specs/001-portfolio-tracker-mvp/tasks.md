---

## description: "Implementation task list for Portfolio Tracker MVP"

# Tasks: Portfolio Tracker MVP

**Feature**: `001-portfolio-tracker-mvp`  
**Inputs**: `specs/001-portfolio-tracker-mvp/spec.md`, `plan.md`, `data-model.md`, `contracts/`  
**Tech**: ASP.NET Core Web API + Clean Architecture, EF Core/Npgsql/Postgres, .NET workers, Next.js (TS), Docker Compose  
**Testing**: xUnit required for critical calculations + domain rules

## Conventions

- Tasks are organized by user story (US1–US5) and the requested UI pages.
- Prefer **ledger as source-of-truth** and deterministic recomputation.
- “Edit” = **auditable correction** (reversal + replacement) for trades and cash entries.
- No broker integrations, no auto-trading, no options/leverage, no tax engine, no streaming prices.

## Phase 1: Setup (Shared Infrastructure)

- T001 Create .NET solution and project files under `src/` (Api/Application/Domain/Infrastructure/Workers) and test projects under `tests/`
- T002 Add baseline build tooling and repo scripts (`dotnet format` config optional) and ensure `dotnet test` runs from repo root
- T003 Ensure Docker Compose Postgres is usable locally via `infra/docker/docker-compose.yml` (ports, volumes, env vars)
- T004 Add EF Core migrations setup in `src/infrastructure/SatellitePortfolio.Infrastructure` (DbContext, migrations assembly)
- T005 Wire dependency injection and configuration in API composition root (`src/api/SatellitePortfolio.Api`)
- T006 Add shared error handling + problem details responses in API (consistent `application/problem+json`)
- T007 Add an `IUserContext` / `ICurrentUser` abstraction (returns constant local user) to keep auth-addable later (no auth in MVP)

---

## Phase 2: Foundational Domain + Data Model (Blocks all user stories)

### Domain modeling (clean architecture)

- T010 [P] Create domain primitives in `src/domain/SatellitePortfolio.Domain/` (Money/EUR, identifiers, time abstractions as needed)
- T011 [P] Define domain entities/value objects for Instruments, Trades, CashLedgerEntries, PriceSnapshots, JournalEntries, Theses, Rules, AlertEvents
- T012 Define correction model for ledger events (reversal + replacement) and invariants (no negative holdings, explicit missing prices behavior)

### Persistence (EF Core + Postgres)

- T020 Implement EF Core models + mappings in `src/infrastructure/SatellitePortfolio.Infrastructure/` and migrations for:
  - Portfolio (single row)
  - Instrument (symbol unique)
  - Trade (immutable + correction metadata)
  - CashLedgerEntry (immutable + correction metadata)
  - PriceSnapshot (instrument+date unique)
  - JournalEntry
  - InvestmentThesis
  - PortfolioRule
  - AlertEvent
- T021 Implement repositories/data access abstractions in `src/application/` (interfaces) and `src/infrastructure/` (implementations)
- T022 Add database initialization / migrations runner for local dev (API startup or a separate migration tool)

### Calculation services (deterministic, testable)

- T030 Implement holdings/positions computation service in `src/domain/` or `src/application/` (choose layer; keep deterministic and pure where possible)
- T031 Implement average-cost cost basis method (MVP choice) and realized/unrealized PnL computation
- T032 Implement allocation % calculation (including cash in denominator by default)
- T033 Implement month-end “as-of” snapshot query strategy (last EOD price on/before date)

### Tests (must exist before API/UI)

- T040 [P] Add xUnit project `tests/SatellitePortfolio.Domain.Tests` with test utilities + deterministic fixtures
- T041 [P] Unit tests for average cost basis + realized PnL across partial sells and fees
- T042 [P] Unit tests for unrealized PnL + allocation with missing price handling
- T043 [P] Unit tests for cash ledger balance + trade cash flows reconciliation
- T044 [P] Unit tests for month-end as-of valuation logic (last price on/before date; missing prices)

**Checkpoint**: Domain model + persistence + core calculations are correct and test-covered.

---

## Phase 3: User Story 1 (P1) — Manual entry (Trades + Cash) with auditable edits

### API (REST)

- T100 Implement Instruments CRUD endpoints in `src/api/SatellitePortfolio.Api` per `contracts/endpoints.md`
- T101 Implement Trades endpoints:
  - `POST /api/trades`
  - `GET /api/trades`
  - `GET /api/trades/{tradeId}`
- T102 Implement Trade correction endpoint:
  - `POST /api/trades/{tradeId}/corrections` (reversal + replacement)
- T103 Implement Cash ledger endpoints:
  - `POST /api/cash/entries`
  - `GET /api/cash/entries`
  - `GET /api/cash/entries/{cashEntryId}`
- T104 Implement Cash correction endpoint:
  - `POST /api/cash/entries/{cashEntryId}/corrections`

### Application layer

- T110 Implement commands/use-cases in `src/application/SatellitePortfolio.Application/` for creating trades/cash entries and creating corrections
- T111 Add validation rules (no sells exceeding holdings; required fields; explicit behavior for negative cash)

### Tests

- T120 [P] Application tests for create trade / create cash entry use cases (happy path)
- T121 [P] Application tests for correction flow (original + reversal + replacement are persisted and linked)
- T122 [P] Application test ensuring selling more than holdings is rejected (or explicitly handled per chosen policy)

**Checkpoint**: Ledger endpoints support create + auditable correction for trades and cash entries.

---

## Phase 4: User Story 2 (P1) — Dashboard + Holdings (derived views)

### API (read models)

- T200 Implement `GET /api/portfolio/overview` (totals, cash, realized/unrealized, allocation summary, current alerts)
- T201 Implement `GET /api/holdings` and `GET /api/holdings/{instrumentId}` derived endpoints
- T202 Ensure missing price behavior is explicit in DTOs (`missingPrice`, explanation)

### Frontend (Next.js)

- T210 Create API client layer in `src/web/satellite-portfolio-web` (typed fetch wrappers + DTO types)
- T211 Implement Overview dashboard page (requested) consuming `portfolio/overview`
- T212 Implement Holdings page (requested) consuming `holdings` endpoints
- T213 UI: show calculation explanations + missing price messaging clearly

### Tests

- T220 [P] Backend tests for overview aggregation (given fixtures → matches expected totals)
- T221 [P] Backend tests for holdings endpoint mapping (missing-price flags; allocation sums)

**Checkpoint**: User can review current positions, cash, PnL, and allocation on dashboard + holdings page.

---

## Phase 5: User Story 3 (P2) — Trades page + Monthly portfolio state

### API

- T300 Implement `GET /api/portfolio/monthly?year=YYYY&month=MM` month-end state endpoint

### Frontend

- T310 Implement Trades page (requested): list/filter/sort + create trade + correct trade flow UI
- T311 Add “monthly state” view in dashboard or dedicated view (as per UX choice) using monthly endpoint

### Tests

- T320 [P] Backend tests for month-end state correctness across month boundaries
- T321 [P] Backend tests for correction visibility in trade history queries

---

## Phase 6: User Story 4 (P2) — Journal entries + Investment theses

### API

- T400 Implement Journal endpoints (list/create/read/update)
- T401 Implement Thesis endpoints (list/create/read/update)
- T402 Implement linking strategy (thesis ↔ instrument; journal ↔ thesis and/or instrument) in application layer

### Frontend

- T410 Implement Journal page (requested): list + create/edit journal entries
- T411 Add Thesis UI: create/edit thesis, link to instrument, and surface linked theses in relevant views

### Tests

- T420 [P] Application tests for linking invariants (thesis must belong to portfolio; instrument link validity)

---

## Phase 7: User Story 5 (P3) — Portfolio rules + Alert events + Workers

### Domain/Application (rule evaluation)

- T500 Implement rule parameter schemas and evaluation logic:
  - max position size (%)
  - max sector concentration (%)
  - drawdown threshold (%)
- T501 Implement alert creation as immutable `AlertEvent` with explanation payload
- T502 Implement “current alerts” query (latest active/triggered per rule)

### Workers (.NET)

- T510 MarketData worker: scheduled EOD snapshot ingest (MVP may be “no-op” + manual entry; keep skeleton for later)
- T511 Risk worker: scheduled rule evaluation job that writes `AlertEvent` records

### API + UI

- T520 Implement Rules endpoints (CRUD + enable/disable)
- T521 Implement Alerts endpoints (`GET /api/alerts`, `GET /api/alerts/current`)
- T522 Implement Rules page (requested): configure thresholds and view current alerts + explanations

### Tests (critical)

- T530 [P] Unit tests for each rule type evaluation (including missing prices behavior)
- T531 [P] Unit tests for drawdown computation on synthetic equity curves
- T532 [P] Worker integration test (or application-level test) for “evaluate → writes AlertEvent”

---

## Phase 8: Price snapshots (EOD) end-to-end (supports holdings + monthly + rules)

- T600 Implement `POST /api/prices/snapshots` and `GET /api/prices/snapshots` endpoints
- T601 Add UI affordance for entering EOD prices (minimal MVP: admin-style form or CSV import later)
- T602 Ensure derived endpoints use EOD snapshots consistently and deterministically
- T603 [P] Tests for price snapshot uniqueness/idempotency and “last available on/before date” behavior

---

## Phase 9: Polish, audit UX, and deployment readiness

- T700 Add audit visibility to UI for corrected entries (show original → reversal → replacement chain and reason)
- T701 Add seed/demo data path for local MVP (optional script) without compromising auditability
- T702 Update repo `README.md` with local run steps (API, web, workers, docker compose)

