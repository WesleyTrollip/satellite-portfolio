# Satellite Portfolio

Single-user satellite portfolio tracker and decision-support platform.

## Goal
Build the tracker first, validate it locally, then use it to support a disciplined satellite portfolio process.

## MVP
- Manual trade entry
- Holdings
- Cost basis
- Realized/unrealized PnL
- Allocation %
- Cash tracking
- Journal/thesis links
- Rules and alerts

## Non-goals
- Auto-trading
- Broker execution
- Options/leverage workflows
- Advanced quant models
- Tax filing engine

## Initial stack
- Backend: ASP.NET Core Web API
- Workers: .NET Worker Services
- Database: PostgreSQL
- Frontend: Next.js + TypeScript

## Development

### Prerequisites

- .NET SDK 9+
- Node.js LTS (for the web app)
- PostgreSQL (local install) or Docker Desktop

### Run PostgreSQL

- Docker option:
  - `cd infra/docker`
  - `docker compose up -d postgres`
- Local PostgreSQL option:
  - Create DB `satellite_portfolio`
  - Ensure credentials match `appsettings.json` or override via `ConnectionStrings__PortfolioDb`

### Run API

- `dotnet run --project src/api/SatellitePortfolio.Api/SatellitePortfolio.Api.csproj`
- Swagger UI: `http://localhost:5014/swagger`

### Run workers

- Market data worker:
  - `dotnet run --project src/workers/SatellitePortfolio.MarketDataWorker/SatellitePortfolio.MarketDataWorker.csproj`
- Risk worker:
  - `dotnet run --project src/workers/SatellitePortfolio.RiskWorker/SatellitePortfolio.RiskWorker.csproj`

### Run web

- `cd src/web/satellite-portfolio-web`
- `npm install`
- `npm run dev`
- Open `http://localhost:3000`

### Seed demo data (optional)

- Start API first, then:
  - `pwsh ./scripts/seed-demo-data.ps1`

### Tests

- Run tests from repo root:
  - `dotnet test`
  - or `pwsh ./scripts/test.ps1`
