# Satellite Portfolio Technical User Guide

This guide is for technical users who need to run, test, and host the application on a local Windows laptop.

## 1) Recommended Local Laptop Approach

For the current MVP, the most practical and reliable setup is:

1. Run PostgreSQL in Docker.
2. Run the API with Kestrel (`dotnet run`).
3. Run the web app with Next.js dev server (`npm run dev`).
4. Run the Risk worker when you need automatic rule evaluation.
5. Use the Market Data worker only if you want heartbeat logging (it is MVP no-op).

Why this is recommended:

- Lowest setup friction.
- Uses the repo's current run paths directly.
- Avoids early IIS/publish complexity unless you specifically need it.

## 2) Prerequisites

- Windows 10/11 laptop
- .NET SDK 9+
- Node.js LTS
- PowerShell 7+ (`pwsh`)
- Docker Desktop (recommended for PostgreSQL)
- Optional: local PostgreSQL install (instead of Docker)

## 3) Architecture At Runtime

- Web UI (`Next.js`) calls API via `NEXT_PUBLIC_API_BASE_URL`.
- API reads/writes PostgreSQL and computes portfolio outputs.
- Risk worker evaluates enabled rules every 15 minutes and writes alerts.
- Market Data worker logs heartbeat only (manual EOD prices are used in MVP).

## 4) Database Setup

### Option A (Recommended): PostgreSQL in Docker

From repo root:

```powershell
cd infra/docker
docker compose up -d postgres
```

Default Docker PostgreSQL values:

- Host: `localhost`
- Port: `5432`
- Database: `satellite_portfolio`
- Username: `satellite`
- Password: `satellite`

### Option B: Local PostgreSQL Install

Create a database named `satellite_portfolio`, then configure API/worker connection strings to match your local credentials.

## 5) Connection String Strategy (Important)

Use one connection string across API and worker services to avoid environment drift.

Recommended override approach (PowerShell session):

```powershell
$env:ConnectionStrings__PortfolioDb = "Host=localhost;Port=5432;Database=satellite_portfolio;Username=satellite;Password=satellite"
```

Then start API/workers in the same shell.

Notes:

- The API appsettings and Docker defaults may not always match in every branch; environment override is the safest local approach.
- API applies EF Core migrations automatically on startup.

## 6) Start the API

From repo root:

```powershell
dotnet run --project src/api/SatellitePortfolio.Api/SatellitePortfolio.Api.csproj
```

Default Kestrel URL: `http://localhost:5014`

Swagger:

- `http://localhost:5014/swagger`

### IIS Express (Visual Studio)

`launchSettings.json` includes:

- Kestrel profile: `http://localhost:5014`
- IIS Express URL: `http://localhost:62356/`

If you run with IIS Express, your API base URL for the web app should be:

- `http://localhost:62356/api`

## 7) Start the Web App

From repo root:

```powershell
cd src/web/satellite-portfolio-web
npm install
```

Create/update `.env.local`:

```env
# Kestrel API
NEXT_PUBLIC_API_BASE_URL=http://localhost:5014/api
```

Or if using IIS Express:

```env
# IIS Express API
NEXT_PUBLIC_API_BASE_URL=http://localhost:62356/api
```

Run web dev server:

```powershell
npm run dev
```

Open: `http://localhost:3000`

## 8) Start Worker Services

From repo root.

### Risk Worker (recommended when validating rules/alerts)

```powershell
dotnet run --project src/workers/SatellitePortfolio.RiskWorker/SatellitePortfolio.RiskWorker.csproj
```

Behavior:

- Runs risk evaluation approximately every 15 minutes.
- Persists triggered alerts.

### Market Data Worker (optional in MVP)

```powershell
dotnet run --project src/workers/SatellitePortfolio.MarketDataWorker/SatellitePortfolio.MarketDataWorker.csproj
```

Behavior:

- Logs heartbeat approximately hourly.
- Does not ingest market data in current MVP.

## 9) Seed Demo Data

API must be running first.

Default (Kestrel API URL):

```powershell
pwsh ./scripts/seed-demo-data.ps1
```

If API is on a different URL (for example IIS Express):

```powershell
pwsh ./scripts/seed-demo-data.ps1 -ApiBaseUrl "http://localhost:62356/api"
```

## 10) Run Tests

From repo root:

```powershell
dotnet test
```

Or use helper script:

```powershell
pwsh ./scripts/test.ps1
```

Watch mode:

```powershell
pwsh ./scripts/test.ps1 -Watch
```

## 11) Hosting Guidance on a Local Laptop

### Current state in this repository

- Docker support currently covers PostgreSQL only.
- No full production-style Dockerfiles/compose setup for API, web, and workers is included yet.
- IIS Express profile is included for local debugging.

### A) Best practical local hosting setup

For most local use, prefer:

- PostgreSQL in Docker
- API via Kestrel process
- Web via `npm run dev` (or `npm run build` + `npm run start` for a production-like Next.js run)
- Risk worker as separate background process

This gives clear logs, easy restart, and minimal packaging overhead.

### B) Local IIS setup (manual)

Use this only if you need IIS specifically.

Typical approach:

1. Publish API:
   - `dotnet publish src/api/SatellitePortfolio.Api/SatellitePortfolio.Api.csproj -c Release -o .tmp/publish/api`
2. Create IIS site/app pool for the published API.
3. Ensure site has access to the same `PortfolioDb` connection string.
4. Keep worker services running as independent background processes (IIS does not host worker services).
5. For web:
   - Keep using Next.js node process (`npm run start`) on localhost and reverse proxy via IIS, or
   - Host static/exported assets only if your frontend mode supports it.

Important:

- This repo does not currently provide a one-click IIS publish script or full IIS reverse-proxy configuration.
- Treat local IIS as an advanced, manual setup path.

### C) Dockerized app stack (future-oriented)

If you want full container hosting on laptop:

- Add Dockerfiles for API, web, and workers.
- Add compose services for each app component.
- Add environment wiring for API URL and DB connection strings.

Until that exists, Docker is best used for PostgreSQL only.

## 12) Troubleshooting

### Web fetch errors / empty page data

- Check `NEXT_PUBLIC_API_BASE_URL` in `.env.local`.
- Confirm API is running and reachable at `/api/...`.
- Restart `npm run dev` after `.env.local` changes.

### API starts but DB errors occur

- Most common issue is connection string mismatch.
- Verify API and worker both use the same `ConnectionStrings__PortfolioDb`.
- Verify Docker Postgres credentials if using compose defaults.

### CORS issues from browser

- Development CORS allows `http://localhost:3000` by default.
- If web runs on a different origin/port, update API CORS config.

### HTTPS or self-signed cert issues

- Use HTTP endpoints for local development where appropriate, or
- Trust local development certificates and use matching HTTPS URLs consistently.
