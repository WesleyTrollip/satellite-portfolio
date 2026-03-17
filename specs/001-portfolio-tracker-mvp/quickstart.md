# Quickstart: Portfolio Tracker MVP (Local Dev)

**Feature**: `001-portfolio-tracker-mvp`  
**Date**: 2026-03-17

## Prerequisites

- .NET SDK 8+
- Node.js (LTS)
- Docker Desktop (for PostgreSQL)

## Local services

From repo root:

```bash
docker compose -f infra/docker/docker-compose.yml up -d
```

## Backend (API)

From repo root:

```bash
# from src/api/SatellitePortfolio.Api once created/populated
dotnet run
```

API should expose Swagger/OpenAPI at `/swagger` in local dev.

## Workers

From repo root:

```bash
# EOD price snapshot job runner (once created/populated)
dotnet run --project src/workers/SatellitePortfolio.MarketDataWorker

# Rule evaluation + alerts (once created/populated)
dotnet run --project src/workers/SatellitePortfolio.RiskWorker
```

## Frontend (Next.js)

From repo root:

```bash
cd src/web/satellite-portfolio-web
npm install
npm run dev
```

## MVP Pages

- Overview dashboard: portfolio totals, cash, alerts, top positions
- Holdings page: positions table + allocation + PnL
- Trades page: trade list + create/edit (auditable correction) flow
- Journal page: entries + thesis linking
- Rules page: configure thresholds + view active alerts

