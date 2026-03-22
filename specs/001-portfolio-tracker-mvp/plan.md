# Implementation Plan: Portfolio Tracker MVP (Monorepo)

**Branch**: `001-portfolio-tracker-mvp` | **Date**: 2026-03-17 | **Spec**: `specs/001-portfolio-tracker-mvp/spec.md`  
**Input**: Feature specification from `specs/001-portfolio-tracker-mvp/spec.md` and clarifications from `specs/001-portfolio-tracker-mvp/clarifications.md`

## Summary

Build a single-user, single-portfolio tracker MVP with manual trades + cash ledger as the source of truth, end-of-day price snapshots, derived positions/holdings/PnL/allocation, thesis journaling, and rule-based risk visibility (max position size, max sector concentration, drawdown). Implement as a monorepo with:

- **Backend**: ASP.NET Core Web API (C#) following clean architecture (API/Application/Domain/Infrastructure).
- **Workers**: .NET Worker Services for scheduled jobs (EOD price snapshot ingest, rule evaluation/alert generation).
- **Database**: PostgreSQL with EF Core + Npgsql; migrations committed.
- **Frontend**: Next.js (TypeScript) consuming REST APIs (no Blazor).
- **Local dev**: Docker Compose for Postgres and supporting services.
- **Testing**: xUnit for backend (domain and application-level rules + calculations).

Authentication is not required for the local MVP, but the architecture must allow adding auth later (e.g., policy-based auth, user context abstraction, separation of auth concerns from domain logic).

## Technical Context

**Language/Version**: C# (.NET 8+) for backend/workers; TypeScript (Node LTS) for frontend  
**Primary Dependencies**: ASP.NET Core Web API, EF Core, Npgsql; Next.js  
**Storage**: PostgreSQL (local via Docker Compose)  
**Testing**: xUnit (backend); frontend tests optional later  
**Target Platform**: Local-first development; deployable to VPS later  
**Project Type**: Monorepo (API + workers + web)  
**Performance Goals**: Small single-user dataset; prioritize correctness and auditability  
**Constraints**: Decision-support only; no broker integration; no auto-trading; no streaming prices; EOD snapshots acceptable

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Decision support only**: No broker execution, no auto-trading, no state mutation by “AI”.
- **MVP-first scope**: Single user, single portfolio, manual entry only; no tax engine; no options/leverage workflows; no real-time prices.
- **Accuracy + auditability**: Ledger is source-of-truth; derived states are reproducible; editing trades uses auditable correction mechanism.
- **Explicit rules + tests**: Cost basis, PnL, allocation, drawdown, and rule evaluation must be explicit and test-covered.
- **Separation of concerns**: Domain independent from Infrastructure/UI; API thin; Workers orchestrate jobs; clean boundaries.

## Project Structure

### Documentation (this feature)

```text
specs/001-portfolio-tracker-mvp/
├── spec.md
├── clarifications.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── README.md
│   ├── openapi.md
│   └── endpoints.md
└── tasks.md        # created by /speckit.tasks after plan is approved
```

### Source Code (repository root)

```text
src/
├── api/
│   └── SatellitePortfolio.Api/                 # ASP.NET Core Web API (Controllers, DTOs, Composition Root)
├── application/
│   └── SatellitePortfolio.Application/         # Use cases, validation, orchestration, query models
├── domain/
│   └── SatellitePortfolio.Domain/              # Entities, value objects, domain services, rule definitions
├── infrastructure/
│   └── SatellitePortfolio.Infrastructure/      # EF Core DbContext, repositories, migrations, adapters
├── workers/
│   ├── SatellitePortfolio.MarketDataWorker/    # EOD price snapshot jobs
│   └── SatellitePortfolio.RiskWorker/          # rule evaluation + alert generation jobs
└── web/
    └── satellite-portfolio-web/                # Next.js app (pages/routes, services, components)

infra/
└── docker/
    └── docker-compose.yml                      # Postgres and supporting services for local dev

tests/
├── SatellitePortfolio.Domain.Tests/            # (create) domain calculations + rules tests
├── SatellitePortfolio.Application.Tests/       # application service tests
└── SatellitePortfolio.Api.Tests/               # thin API tests (optional early)
```

**Structure Decision**: Keep the monorepo structure above; clean architecture layers compiled separately; API depends on Application; Application depends on Domain; Infrastructure depends on Domain/Application; Workers depend on Application/Infrastructure; Web depends on API contracts only.

## Phase 0: Research (target: 0.5–1 day)

Goals:
- Confirm EF Core mapping strategy for auditable “correction” model (reversal+replacement vs versioned records).
- Decide cost basis method for MVP (recommend **average cost** for simplicity; FIFO later).
- Decide how EOD snapshots are sourced in MVP (manual import/upload vs stubbed input) while keeping contracts stable.

Output: `research.md` (trade-offs + decision log).

## Phase 1: Design (target: 1–2 days)

### 1) Data model design (Postgres + EF Core)

Design tables/entities for:
- **Portfolio** (single row)
- **Instrument** (symbol, name, sector, currency=EUR)
- **Trade** (buy/sell) with audit metadata and correction linkage
- **CashLedgerEntry** with audit metadata and correction linkage
- **PriceSnapshot** (symbol, date, close price in EUR; EOD)
- **JournalEntry** and **Thesis** (linked to instruments/positions/trades as needed)
- **PortfolioRule** (max position size, max sector concentration, drawdown threshold)
- **AlertEvent** (derived evaluation results; immutable)

Key decisions to encode:
- **Editing trades**: implement **reversal + replacement** (preferred) or **versioned records**. Either must be queryable and reproducible.
- **Derived state**: store minimal derived summaries (optional) but keep canonical ledger so positions can always be recomputed.
- **Month-end snapshots**: computed query (as-of end-of-month) using ledger + last available EOD price on/before date.

Output: `data-model.md` (entity definitions, relationships, invariants, and EF Core mapping notes).

### 2) REST API contracts

Define REST resources aligned to UI pages and backend use-cases:
- **Portfolio**: read overview (totals, cash, allocation, alerts)
- **Instruments**: CRUD for instruments + sector assignment
- **Trades**: CRUD + correction flow
- **Cash ledger**: CRUD + correction flow
- **Positions/Holdings**: read-only derived views
- **Price snapshots**: CRUD/import for EOD snapshots
- **Journal/Theses**: CRUD + linking to instruments and (optionally) trades
- **Rules**: CRUD + enable/disable
- **Alerts**: read alert history and current alerts
- **Monthly state**: read month-end portfolio state

Output: `contracts/endpoints.md` + `contracts/openapi.md` (OpenAPI-first outline; generated swagger later from ASP.NET).

### 3) UI page design (Next.js)

Pages to implement first:
- **Overview dashboard**
- **Holdings page**
- **Trades page** (includes edit/correct workflow)
- **Journal page**
- **Rules page**

UI principles:
- Clarity > density; show explanations for derived numbers and rule evaluations.
- Editing/corrections must be transparent: show original vs corrected and why.

Output: brief UI interaction notes in `quickstart.md` and/or a section in `research.md`.

## Phase 2: Implementation Tasks (created by /speckit.tasks)

Tasks will be grouped by user story, with xUnit coverage for critical calculations:
- Cost basis + realized/unrealized PnL
- Allocation %
- Drawdown computation
- Rule evaluation and alert generation
- Correction/editing workflow auditability

## Non-Goals (enforced)

- No broker integrations; no order placement; no auto-trading.
- No options/leverage workflows.
- No tax engine.
- No real-time streaming prices.
- No advanced AI features in MVP.

