# Satellite Portfolio UI User Guide

This guide explains how to run and use the current MVP UI.

## 1) Before You Start

The UI depends on the API. Start both in this order:

1. Start PostgreSQL (Docker or local install)
2. Start API
3. Start web app

### API URL used by the web app

Set `NEXT_PUBLIC_API_BASE_URL` in `src/web/satellite-portfolio-web/.env.local`.

Example with IIS Express HTTP:

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:62356/api
```

Example with Kestrel:

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5014/api
```

## 2) Running the Application

### Start API

Use one of:

- Visual Studio (F5) with your preferred profile
- CLI:
  - `dotnet run --project src/api/SatellitePortfolio.Api/SatellitePortfolio.Api.csproj`

Swagger is available at:

- `http://localhost:5014/swagger` (CLI default profile)
- or your IIS Express URL in `launchSettings.json`

### Start web UI

From `src/web/satellite-portfolio-web`:

- `npm install`
- `npm run dev`

Open:

- `http://localhost:3000`

## 3) Top Navigation

Current UI pages:

- **Overview**
- **Holdings**
- **Trades**
- **Prices**
- **Journal**
- **Rules**

## 4) Page-by-Page Guide

## Overview

What it shows:

- Current portfolio totals:
  - portfolio value
  - cash balance
  - total market value
  - realized PnL
  - unrealized PnL
- Current active alerts
- Month-end snapshot summary

Backed by:

- `GET /api/portfolio/overview`
- `GET /api/portfolio/monthly`

## Holdings

What it shows per instrument:

- quantity
- average cost
- market value
- unrealized PnL
- allocation %
- pricing status
- linked thesis titles

Missing price handling:

- If no price exists for as-of valuation, the row shows explicit missing-price text.

Backed by:

- `GET /api/holdings`
- `GET /api/theses`

## Trades

Purpose:

- Create manual trades
- Submit auditable trade corrections
- View history and correction chains

### Create Trade

Required fields:

- Instrument (lookup select)
- Side (`Buy` or `Sell`)
- Quantity
- Price
- Fees
- Executed At

### Correct Trade

Required fields:

- Selected trade row (`Edit` action)
- Corrected quantity/price/fees/executedAt
- Correction reason (lookup select)

How corrections appear:

- UI shows grouped correction audit chains:
  - reversal entry
  - replacement entry
  - selected correction reason label

Backed by:

- `GET /api/trades`
- `POST /api/trades`
- `POST /api/trades/{tradeId}/corrections`

## Prices

Purpose:

- Enter or update end-of-day (EOD) prices
- Filter and view snapshot history

### Add / Update Snapshot

Required fields:

- Instrument (lookup select)
- Date
- Close Price
- Source (lookup select)

Behavior:

- Upsert is idempotent by `(instrumentId, date)`.
- Posting same instrument/date updates existing snapshot.
- Use row-level `Edit` in snapshot results to preload update form values.

Backed by:

- `GET /api/prices/snapshots`
- `POST /api/prices/snapshots`

## Journal

Purpose:

- Create theses
- Create journal entries
- Link journals to thesis/instrument context

### Create Thesis

Fields:

- title
- body
- status (`Active` / `Retired`)
- optional linked instrument (lookup select)

### Create / Update Journal Entry

Fields:

- occurredAt
- title
- body
- tags
- optional linked thesis (lookup select)
- optional linked instrument (lookup select)

Update behavior:

- Select a row in `Journal Entries` and click `Edit`.
- The update form preloads existing values.
- Use `Cancel` to reset edit mode.

Backed by:

- `GET /api/journal`
- `POST /api/journal`
- `PUT /api/journal/{journalEntryId}`
- `GET /api/theses`
- `POST /api/theses`
- `PUT /api/theses/{thesisId}`

## Rules

Purpose:

- Configure risk rules
- View current alerts

### Rule types

- Max position size
- Max sector concentration
- Max drawdown

### Create / Update Rule

Fields:

- rule type
- enabled flag
- parameters JSON

Update behavior:

- Update always starts from `Current Rules` table row `Edit`.
- Selected rule is preloaded into the update form.
- Use `Cancel` to clear the selection.

Example `parametersJson`:

- Position size: `{"maxPct":0.10}`
- Sector concentration: `{"maxPct":0.25}`
- Drawdown: `{"maxDrawdownPct":0.15}`

Backed by:

- `GET /api/rules`
- `POST /api/rules`
- `PUT /api/rules/{ruleId}`
- `GET /api/alerts/current`

## 5) Typical User Workflow

Recommended sequence:

1. Create/maintain lookup data (sectors, price sources, correction reasons)
2. Add instruments (linked to sector lookup where applicable)
3. Add initial cash entries
4. Create buy/sell trades from instrument lookup selection
5. Enter EOD prices using instrument + source lookup selections
6. Review Overview and Holdings
7. Add journal/thesis context via lookup selections
8. Configure risk rules and monitor alerts

## 6) Demo Data (Optional)

You can seed sample data after API is running:

- `pwsh ./scripts/seed-demo-data.ps1`

What it seeds:

- instruments
- initial cash
- sample trades
- sample price snapshots
- baseline rule

## 7) Troubleshooting

## UI shows fetch errors (500 on `/`)

Common cause:

- API base URL mismatch in `.env.local`

Fix:

- Verify `NEXT_PUBLIC_API_BASE_URL` matches your running API port/profile.
- Restart web dev server after changing `.env.local`.

## HTTPS certificate errors in web server-side fetch

Symptom:

- `DEPTH_ZERO_SELF_SIGNED_CERT`

Fix options:

- Use HTTP base URL for IIS Express profile, or
- Trust dev certs and use matching HTTPS endpoint.

## Empty dashboard/holdings

Expected if no data exists. Add:

- cash entries
- trades
- prices

## 8) Current MVP Notes

- This is a decision-support tracker, not an execution platform.
- Corrections are auditable and shown as chain events, not silent overwrites.
- Normal user workflows avoid manual GUID entry; IDs are selected from lookup-backed lists/tables.

## 9) Lookup Management

Lookup values are managed through API CRUD and consumed by the UI. Records can be updated to inactive (`isActive=false`) or deleted when unreferenced.

### Admin UI

Use the `Admin` page in the top navigation to manage:

- sectors
- price sources
- correction reasons
- instruments

Each table supports:

- row `Edit` to preload form fields
- `Save` to update
- `Cancel` to reset form state
- `Delete` to remove records when unreferenced

Delete behavior:

- Deletes are hard-delete attempts.
- If a record is still referenced (for example by trades, prices, journal, or theses), the API returns conflict and the UI shows the message.

### Sector lookups

- `GET /api/lookups/sectors?search=&isActive=&skip=&take=`
- `GET /api/lookups/sectors/{id}`
- `POST /api/lookups/sectors`
- `PUT /api/lookups/sectors/{id}`
- `DELETE /api/lookups/sectors/{id}` (hard delete, blocked when referenced)

### Price source lookups

- `GET /api/lookups/price-sources?search=&isActive=&skip=&take=`
- `GET /api/lookups/price-sources/{id}`
- `POST /api/lookups/price-sources`
- `PUT /api/lookups/price-sources/{id}`
- `DELETE /api/lookups/price-sources/{id}` (hard delete, blocked when referenced)

### Correction reason lookups

- `GET /api/lookups/correction-reasons?search=&isActive=&skip=&take=`
- `GET /api/lookups/correction-reasons/{id}`
- `POST /api/lookups/correction-reasons`
- `PUT /api/lookups/correction-reasons/{id}`
- `DELETE /api/lookups/correction-reasons/{id}` (hard delete, blocked when referenced)

### Instruments (master data)

- `GET /api/instruments`
- `GET /api/instruments/{instrumentId}`
- `POST /api/instruments`
- `PUT /api/instruments/{instrumentId}`
- `DELETE /api/instruments/{instrumentId}` (hard delete, blocked when referenced)

