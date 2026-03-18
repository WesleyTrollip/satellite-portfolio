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

- Run tests from the repo root:
  - `dotnet test`
  - or `pwsh ./scripts/test.ps1`
